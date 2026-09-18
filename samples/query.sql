WITH RECURSIVE organizational_hierarchy AS (
    SELECT id, parent_organization_id, name, 1 AS hierarchy_level, ARRAY[id::text] AS ancestry_path, FALSE AS is_cycle
    FROM core_organizations WHERE deleted_at IS NULL AND status = 'ACTIVE'
    UNION ALL
    SELECT o.id, o.parent_organization_id, o.name, oh.hierarchy_level + 1, oh.ancestry_path || o.id::text, o.id::text = ANY(oh.ancestry_path)
    FROM core_organizations o
    INNER JOIN organizational_hierarchy oh ON o.parent_organization_id = oh.id
    WHERE NOT oh.is_cycle AND o.deleted_at IS NULL AND o.status = 'ACTIVE'
),
unfiltered_financial_ledger AS (
    SELECT 
        l.id AS ledger_id, l.organization_id, l.currency_code, l.amount_cents, l.tax_rate_basis_points, l.transaction_type, l.metadata, l.posted_at,
        CASE 
            WHEN l.transaction_type IN ('CREDIT', 'REFUND_REVERSAL', 'INBOUND_WIRE') THEN 1 
            WHEN l.transaction_type IN ('DEBIT', 'CHARGEBACK', 'OUTBOUND_ACH', 'PLATFORM_FEE') THEN -1 
            ELSE 0 
        END AS financial_direction_multiplier,
        COALESCE((l.metadata->'billing_address'->>'country_code'), 'US') AS normalized_country,
        LEAST(GREATEST(CAST(l.metadata->'risk_scores'->>'fraud_index' AS NUMERIC), 0.0), 100.0) AS cleaned_fraud_score
    FROM accounting_ledgers l
    WHERE l.posted_at >= TIMESTAMPTZ '2025-01-01 00:00:00Z' - INTERVAL '1 year' 
      AND l.posted_at <= TIMESTAMPTZ '2026-12-31 23:59:59Z'
      AND l.lifecycle_state NOT IN ('VOIDED', 'PENDING_VERIFICATION')
),
calculated_metrics_stage AS (
    SELECT 
        fl.organization_id, fl.currency_code,
        SUM(fl.amount_cents * fl.financial_direction_multiplier) AS net_volume_cents,
        SUM(fl.amount_cents) FILTER (WHERE fl.financial_direction_multiplier = 1) AS gross_inflow_cents,
        SUM(fl.amount_cents) FILTER (WHERE fl.financial_direction_multiplier = -1) AS gross_outflow_cents,
        COUNT(DISTINCT fl.ledger_id) AS total_processed_transactions,
        COUNT(fl.ledger_id) FILTER (WHERE fl.cleaned_fraud_score > 75.5 AND fl.transaction_type = 'CHARGEBACK') AS high_risk_chargebacks,
        AVG(fl.amount_cents) OVER(PARTITION BY fl.organization_id, fl.currency_code ORDER BY fl.posted_at ROWS BETWEEN 30 PRECEDING AND CURRENT ROW) AS rolling_avg_amount_30_day,
        PERCENTILE_CONT(0.95) WITHIN GROUP (ORDER BY fl.amount_cents) AS p95_transaction_size,
        STDDEV_SAMP(fl.amount_cents) AS transaction_standard_deviation
    FROM unfiltered_financial_ledger fl
    GROUP BY fl.organization_id, fl.currency_code, fl.posted_at, fl.amount_cents
),
flattened_json_metadata AS (
    SELECT 
        fl.organization_id,
        jsonb_object_agg(COALESCE(fl.metadata->>'campaign_id', 'organic'), jsonb_build_object(
            'total_attributed_cents', fl.amount_cents,
            'captured_at_iso', TO_CHAR(fl.posted_at, 'YYYY-MM-DD"T"HH24:MI:SS"Z"'),
            'geo_routing_data', jsonb_build_array(fl.normalized_country, fl.metadata->'routing'->>'gateway_node')
        )) AS marketing_attribution_map
    FROM unfiltered_financial_ledger fl
    WHERE fl.metadata IS NOT NULL AND jsonb_typeof(fl.metadata) = 'object'
    GROUP BY fl.organization_id
)
SELECT 
    oh.id AS root_org_id, oh.name AS root_org_name, oh.hierarchy_level,
    ARRAY_TO_STRING(oh.ancestry_path, ' -> ') AS human_readable_path,
    cms.currency_code,
    COALESCE(cms.net_volume_cents, 0) / 100.0 AS net_volume_standard_unit,
    ROUND((cms.gross_inflow_cents::numeric / NULLIF(cms.gross_outflow_cents, 0)::numeric) * 100, 4) AS inflow_to_outflow_ratio_percentage,
    cms.total_processed_transactions,
    (cms.high_risk_chargebacks::numeric / NULLIF(cms.total_processed_transactions, 0)::numeric) * 100.0 AS critical_fraud_ratio,
    cms.p95_transaction_size,
    cms.transaction_standard_deviation,
    fjm.marketing_attribution_map,
    CASE 
        WHEN cms.net_volume_cents > 500000000 THEN 'TIER_1_ENTERPRISE'
        WHEN cms.net_volume_cents BETWEEN 100000000 AND 500000000 THEN 'TIER_2_MID_MARKET'
        WHEN cms.net_volume_cents < 100000000 AND cms.high_risk_chargebacks > 5 THEN 'TIER_3_HIGH_RISK_SMB'
        ELSE 'TIER_4_STANDARD_SMB'
    END AS automated_tenant_classification,
    DENSE_RANK() OVER (PARTITION BY oh.hierarchy_level, cms.currency_code ORDER BY cms.net_volume_cents DESC NULLS LAST) AS volume_rank_within_tier,
    LAG(cms.net_volume_cents, 1, 0) OVER (PARTITION BY oh.id, cms.currency_code ORDER BY cms.net_volume_cents) AS previous_period_delta_marker
FROM organizational_hierarchy oh
INNER JOIN calculated_metrics_stage cms ON oh.id = cms.organization_id
LEFT JOIN flattened_json_metadata fjm ON oh.id = fjm.organization_id
WHERE oh.hierarchy_level <= 5 
  AND NOT oh.is_cycle
  AND (cms.currency_code IN ('USD', 'EUR', 'GBP', 'CAD', 'AUD') OR cms.net_volume_cents > 10000000)
ORDER BY oh.hierarchy_level ASC, volume_rank_within_tier DESC, root_org_id
LIMIT 1000 OFFSET 0;
