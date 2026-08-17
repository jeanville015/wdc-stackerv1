# Database Schema

## BOX.BOXDETAILS (BLACKBOX)

```
USE [BOXMANAGEMENT]
GO

/****** Object:  Table [BOX].[BOXDETAILS]    Script Date: 8/9/2026 11:04:58 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [BOX].[BOXDETAILS](
	[BOXNO] [varchar](50) NULL,
	[RACKNUM] [smallint] NULL,
	[LAYERROWNUM] [smallint] NULL,
	[LAYERCOLNUM] [smallint] NULL,
	[BOXSTATUS] [varchar](50) NULL,
	[UPDATEBY] [varchar](50) NULL,
	[UPDATETS] [datetime] NULL,
	[STORETS] [timestamp] NULL,
	[CLIENTCODE] [varchar](50) NULL,
	[PENNUM] [varchar](50) NULL,
	[PARTNUM] [varchar](50) NULL,
	[PRODUCTNAME] [nchar](10) NULL
) ON [PRIMARY]
GO
```

## BOX.HOLDER_ASSIGN (HOLDERS)

```
USE [BOXMANAGEMENT]
GO

/****** Object:  Table [BOX].[HOLDER_ASSIGN]    Script Date: 8/9/2026 11:05:19 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [BOX].[HOLDER_ASSIGN](
	[HOLDER] [varchar](50) NULL,
	[BOXSTATUS] [varchar](max) NULL,
	[BOXNAME] [varchar](50) NULL,
	[RACKNUM] [smallint] NULL,
	[LAYERROWNUM] [smallint] NULL,
	[LAYERCOLNUM] [smallint] NULL,
	[UPDATEBY] [varchar](50) NULL,
	[UPDATETS] [datetime] NULL,
	[STORETS] [timestamp] NULL,
	[QTY] [smallint] NULL,
	[PRODUCTNAME] [nchar](10) NULL,
	[WORKFLOW] [varchar](max) NULL,
	[CLASSCODE] [varchar](50) NULL,
	[FACTORY] [varchar](10) NULL,
	[LEC] [varchar](10) NULL,
	[EXPERIMENT] [varchar](10) NULL,
	[MINORREV] [varchar](10) NULL,
	[PROCESS] [varchar](max) NULL,
	[STATUS] [varchar](max) NULL,
	[JOB] [int] NULL,
	[SHIPBOXNAME] [varchar](10) NULL,
	[PARTNUM] [varchar](50) NULL,
	[PENNUM] [varchar](50) NULL,
	[GRADE] [varchar](50) NULL,
	[CAMVERSION] [varchar](10) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
```

## BOX.SHIPBOXDETAILS (SHIPBOX)

```
USE [BOXMANAGEMENT]
GO

/****** Object:  Table [BOX].[SHIPBOXDETAILS]    Script Date: 8/9/2026 11:06:08 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [BOX].[SHIPBOXDETAILS](
	[SHIPBOXNAME] [varchar](50) NULL,
	[BOXNO] [varchar](50) NULL,
	[SHIPBOXSTATUS] [varchar](50) NULL,
	[UPDATEBY] [varchar](50) NULL,
	[UPDATETS] [datetime] NULL,
	[STORETS] [timestamp] NULL,
	[SHIPBOXNUM] [smallint] NULL,
	[LAYERROWNUM] [smallint] NULL,
	[LAYERCOLNUM] [smallint] NULL,
	[LEC] [varchar](50) NULL
) ON [PRIMARY]
GO
```
