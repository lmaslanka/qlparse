-- expect: Script(TsqlUseStatement | TsqlSetOptionStatement | TsqlSetOptionStatement | CreateTableStatement | CreateIndexStatement | AlterTableStatement | AlterTableStatement | AlterTableStatement | AlterTableStatement | AlterTableStatement | AlterTableStatement)
USE [SalesDB]
GO
/****** Object:  Table [Sales].[OrderHeader]    Script Date: 3/14/2024 10:22:41 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Sales].[OrderHeader](
	[OrderID] [int] IDENTITY(1,1) NOT NULL,
	[CustomerID] [int] NOT NULL,
	[OrderDate] [datetime2](3) NOT NULL,
	[DueDate] [date] NULL,
	[Status] [tinyint] NOT NULL,
	[TotalDue] [money] NOT NULL,
	[Comment] [nvarchar](400) NULL,
 CONSTRAINT [PK_OrderHeader] PRIMARY KEY CLUSTERED 
(
	[OrderID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IX_OrderHeader_CustomerID]    Script Date: 3/14/2024 10:22:41 AM ******/
CREATE NONCLUSTERED INDEX [IX_OrderHeader_CustomerID] ON [Sales].[OrderHeader]
(
	[CustomerID] ASC,
	[OrderDate] DESC
)
INCLUDE([Status],[TotalDue]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [Sales].[OrderHeader] ADD  CONSTRAINT [DF_OrderHeader_OrderDate]  DEFAULT (sysdatetime()) FOR [OrderDate]
GO
ALTER TABLE [Sales].[OrderHeader] ADD  CONSTRAINT [DF_OrderHeader_TotalDue]  DEFAULT ((0.00)) FOR [TotalDue]
GO
ALTER TABLE [Sales].[OrderHeader]  WITH CHECK ADD  CONSTRAINT [FK_OrderHeader_Customer_CustomerID] FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customer] ([CustomerID])
GO
ALTER TABLE [Sales].[OrderHeader] CHECK CONSTRAINT [FK_OrderHeader_Customer_CustomerID]
GO
ALTER TABLE [Sales].[OrderHeader]  WITH NOCHECK ADD  CONSTRAINT [CK_OrderHeader_DueDate] CHECK  (([DueDate]>=CONVERT([date],[OrderDate])))
GO
ALTER TABLE [Sales].[OrderHeader] NOCHECK CONSTRAINT [CK_OrderHeader_DueDate]
GO
