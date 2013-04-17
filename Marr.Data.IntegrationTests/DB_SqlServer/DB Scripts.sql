USE [master]
GO
/****** Object:  Database [MarrDataSqlServerTests]    Script Date: 01/02/2012 17:02:08 ******/
CREATE DATABASE [MarrDataSqlServerTests] ON  PRIMARY 
( NAME = N'MarrDataSqlServerTests', FILENAME = N'c:\Program Files\Microsoft SQL Server\MSSQL10_50.SQLEXPRESS\MSSQL\DATA\MarrDataSqlServerTests.mdf' , SIZE = 3072KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'MarrDataSqlServerTests_log', FILENAME = N'c:\Program Files\Microsoft SQL Server\MSSQL10_50.SQLEXPRESS\MSSQL\DATA\MarrDataSqlServerTests_log.ldf' , SIZE = 1024KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [MarrDataSqlServerTests] SET COMPATIBILITY_LEVEL = 100
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [MarrDataSqlServerTests].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [MarrDataSqlServerTests] SET ANSI_NULL_DEFAULT OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET ANSI_NULLS OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET ANSI_PADDING OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET ANSI_WARNINGS OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET ARITHABORT OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET AUTO_CLOSE OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET AUTO_CREATE_STATISTICS ON
GO
ALTER DATABASE [MarrDataSqlServerTests] SET AUTO_SHRINK OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET AUTO_UPDATE_STATISTICS ON
GO
ALTER DATABASE [MarrDataSqlServerTests] SET CURSOR_CLOSE_ON_COMMIT OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET CURSOR_DEFAULT  GLOBAL
GO
ALTER DATABASE [MarrDataSqlServerTests] SET CONCAT_NULL_YIELDS_NULL OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET NUMERIC_ROUNDABORT OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET QUOTED_IDENTIFIER OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET RECURSIVE_TRIGGERS OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET  DISABLE_BROKER
GO
ALTER DATABASE [MarrDataSqlServerTests] SET AUTO_UPDATE_STATISTICS_ASYNC OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET DATE_CORRELATION_OPTIMIZATION OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET TRUSTWORTHY OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET ALLOW_SNAPSHOT_ISOLATION OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET PARAMETERIZATION SIMPLE
GO
ALTER DATABASE [MarrDataSqlServerTests] SET READ_COMMITTED_SNAPSHOT OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET HONOR_BROKER_PRIORITY OFF
GO
ALTER DATABASE [MarrDataSqlServerTests] SET  READ_WRITE
GO
ALTER DATABASE [MarrDataSqlServerTests] SET RECOVERY SIMPLE
GO
ALTER DATABASE [MarrDataSqlServerTests] SET  MULTI_USER
GO
ALTER DATABASE [MarrDataSqlServerTests] SET PAGE_VERIFY CHECKSUM
GO
ALTER DATABASE [MarrDataSqlServerTests] SET DB_CHAINING OFF
GO
USE [MarrDataSqlServerTests]
GO
/****** Object:  User [jmarr]    Script Date: 01/02/2012 17:02:08 ******/
CREATE USER [jmarr] FOR LOGIN [jmarr] WITH DEFAULT_SCHEMA=[dbo]
GO
/****** Object:  Table [dbo].[Receipt]    Script Date: 01/02/2012 17:02:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Receipt](
	[OrderItemID] [int] NULL,
	[AmountPaid] [decimal](18, 0) NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OrderItem]    Script Date: 01/02/2012 17:02:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[OrderItem](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OrderID] [int] NULL,
	[ItemDescription] [varchar](50) NULL,
	[Price] [decimal](18, 0) NULL,
 CONSTRAINT [PK_OrderItem] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Order]    Script Date: 01/02/2012 17:02:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Order](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OrderName] [varchar](50) NULL,
 CONSTRAINT [PK_Order] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
/****** Object:  View [dbo].[V_OrdersReceipts]    Script Date: 01/02/2012 17:02:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[V_OrdersReceipts]
AS
SELECT     o.ID, o.OrderName, oi.ID AS Expr1, oi.OrderID, oi.ItemDescription, oi.Price, r.OrderItemID, r.AmountPaid
FROM         dbo.[Order] AS o LEFT OUTER JOIN
                      dbo.OrderItem AS oi ON o.ID = oi.OrderID LEFT OUTER JOIN
                      dbo.Receipt AS r ON oi.ID = r.OrderItemID
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "o"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 93
               Right = 198
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "oi"
            Begin Extent = 
               Top = 6
               Left = 236
               Bottom = 123
               Right = 400
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "r"
            Begin Extent = 
               Top = 6
               Left = 438
               Bottom = 93
               Right = 598
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'V_OrdersReceipts'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'V_OrdersReceipts'
GO
/****** Object:  View [dbo].[V_Orders]    Script Date: 01/02/2012 17:02:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[V_Orders]
AS
SELECT     o.ID, o.OrderName, oi.ID AS OrderItemID, oi.OrderID, oi.ItemDescription, oi.Price
FROM         dbo.[Order] AS o LEFT OUTER JOIN
                      dbo.OrderItem AS oi ON o.ID = oi.OrderID
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "o"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 93
               Right = 198
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "oi"
            Begin Extent = 
               Top = 6
               Left = 236
               Bottom = 123
               Right = 400
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'V_Orders'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'V_Orders'
GO
