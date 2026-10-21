-- expect: Script(TsqlUseStatement | TsqlSetOptionStatement | TsqlSetOptionStatement | CreateTableStatement | AlterTableStatement | AlterTableStatement | AlterTableStatement | AlterTableStatement | AlterTableStatement | TsqlExecuteStatement | TsqlExecuteStatement)
USE [SalesDB]
GO
/****** Object:  Table [dbo].[Customer]    Script Date: 3/14/2024 10:22:41 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Customer](
	[CustomerID] [int] IDENTITY(1,1) NOT NULL,
	[AccountNumber] [varchar](10) NOT NULL,
	[FirstName] [nvarchar](50) NOT NULL,
	[LastName] [nvarchar](50) NOT NULL,
	[EmailAddress] [nvarchar](256) NULL,
	[Notes] [nvarchar](max) NULL,
	[Status] [tinyint] NOT NULL,
	[ModifiedDate] [datetime] NOT NULL,
	[rowguid] [uniqueidentifier] ROWGUIDCOL  NOT NULL,
 CONSTRAINT [PK_Customer_CustomerID] PRIMARY KEY CLUSTERED 
(
	[CustomerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[Customer] ADD  CONSTRAINT [DF_Customer_Status]  DEFAULT ((1)) FOR [Status]
GO
ALTER TABLE [dbo].[Customer] ADD  CONSTRAINT [DF_Customer_ModifiedDate]  DEFAULT (getdate()) FOR [ModifiedDate]
GO
ALTER TABLE [dbo].[Customer] ADD  CONSTRAINT [DF_Customer_rowguid]  DEFAULT (newid()) FOR [rowguid]
GO
ALTER TABLE [dbo].[Customer]  WITH CHECK ADD  CONSTRAINT [CK_Customer_Status] CHECK  (([Status]>=(0) AND [Status]<=(3)))
GO
ALTER TABLE [dbo].[Customer] CHECK CONSTRAINT [CK_Customer_Status]
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Primary key for Customer records.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Customer', @level2type=N'COLUMN',@level2name=N'CustomerID'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Current customer information.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'Customer'
GO
