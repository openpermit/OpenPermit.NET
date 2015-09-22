USE [openpermit]
GO

/****** Object:  Table [dbo].[Permit]    Script Date: 9/22/2015 12:37:37 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[Permit](
	[PermitNum] [varchar](100) NOT NULL,
	[Description] [varchar](100) NULL,
	[IssuedDate] [datetime2](7) NULL,
	[CompletedDate] [datetime2](7) NULL,
	[StatusCurrent] [varchar](20) NULL,
	[OriginalAddress1] [varchar](50) NULL,
	[OriginalAddress2] [varchar](50) NULL,
	[OriginalCity] [varchar](50) NULL,
	[OriginalState] [varchar](50) NULL,
	[OriginalZip] [varchar](20) NULL,
	[Jurisdiction] [varchar](100) NULL,
	[PermitClass] [varchar](50) NULL,
	[PermitClassMapped] [varchar](50) NULL,
	[StatusCurrentMapped] [varchar](50) NULL,
	[AppliedDate] [datetime2](7) NULL,
	[WorkClass] [varchar](50) NULL,
	[WorkClassMapped] [varchar](50) NULL,
	[PermitType] [varchar](20) NULL,
	[PermitTypeMapped] [varchar](20) NULL,
	[PermitTypeDesc] [varchar](100) NULL,
	[StatusDate] [datetime2](7) NULL,
	[TotalSqFt] [int] NULL,
	[Link] [varchar](100) NULL,
	[Latitude] [real] NULL,
	[Longitude] [real] NULL,
	[EstProjectCost] [real] NULL,
	[HousingUnits] [int] NULL,
	[PIN] [varchar](50) NULL,
	[ContractorCompanyName] [varchar](100) NULL,
	[ContractorTrade] [varchar](50) NULL,
	[ContractorTradeMapped] [varchar](50) NULL,
	[ContractorLicNum] [varchar](50) NULL,
	[ContractorStateLic] [varchar](100) NULL,
	[ProposedUse] [varchar](100) NULL,
	[AddedSqFt] [int] NULL,
	[RemovedSqFt] [int] NULL,
	[ExpiresDate] [datetime2](7) NULL,
	[COIssuedDate] [datetime2](7) NULL,
	[HoldDate] [datetime2](7) NULL,
	[VoidDate] [datetime2](7) NULL,
	[ProjectName] [varchar](50) NULL,
	[ProjectId] [varchar](100) NULL,
	[TotalFinishedSqFt] [int] NULL,
	[TotalUnfinishedSqFt] [int] NULL,
	[TotalHeatedSqFt] [int] NULL,
	[TotalUnheatedSqFt] [int] NULL,
	[TotalAccSqFt] [int] NULL,
	[TotalSprinkledSqFt] [int] NULL,
	[ExtraFields] [varchar](max) NULL,
	[Publisher] [varchar](50) NULL,
	[Fee] [float] NULL,
	[ContractorFullName] [varchar](50) NULL,
	[ContractorCompanyDesc] [varchar](100) NULL,
	[ContractorPhone] [varchar](20) NULL,
	[ContractorAddress1] [varchar](50) NULL,
	[ContractorAddress2] [varchar](50) NULL,
	[ContractorCity] [varchar](50) NULL,
	[ContractorState] [varchar](50) NULL,
	[ContractorZip] [varchar](20) NULL,
	[ContractorEmail] [varchar](100) NULL,
 CONSTRAINT [PK_Permit] PRIMARY KEY CLUSTERED 
(
	[PermitNum] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

