
CREATE TABLE [dbo].[PermitStatus](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[PermitNum] [varchar](100) NOT NULL,
	[StatusPrevious] [varchar](20) NULL,
	[StatusPreviousDate] [datetime2](7) NULL,
	[StatusPreviousMapped] [varchar](20) NULL,
	[Comments] [varchar](100) NULL,
 CONSTRAINT [PK_PermitStatus] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

