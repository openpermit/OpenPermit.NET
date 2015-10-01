
CREATE TABLE [dbo].[Inspection](
	[UniqueId] [int] IDENTITY(1,1) NOT NULL,
	[PermitNum] [varchar](100) NOT NULL,
	[InspType] [varchar](50) NULL,
	[InspTypeMapped] [varchar](50) NULL,
	[Result] [varchar](100) NULL,
	[ResultMapped] [varchar](100) NULL,
	[ScheduledDate] [datetime2](7) NULL,
	[InspectedDate] [datetime2](7) NULL,
	[InspectionNotes] [varchar](100) NULL,
	[Description] [varchar](100) NULL,
	[Final] [int] NULL,
	[RequestDate] [datetime2](7) NULL,
	[DesiredDate] [datetime2](7) NULL,
	[ReInspection] [int] NULL,
	[Inspector] [varchar](50) NULL,
	[ExtraFields] [varchar](max) NULL,
	[Id] [varchar](50) NULL,
 CONSTRAINT [PK_Inspection] PRIMARY KEY CLUSTERED 
(
	[UniqueId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
