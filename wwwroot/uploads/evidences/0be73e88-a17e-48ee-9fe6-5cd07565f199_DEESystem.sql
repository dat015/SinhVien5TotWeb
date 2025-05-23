USE [DEE_System]
GO
/****** Object:  Table [dbo].[Assignments]    Script Date: 5/15/2025 6:08:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Assignments](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[CourseId] [uniqueidentifier] NOT NULL,
	[Title] [nvarchar](255) NOT NULL,
	[Content] [nvarchar](max) NULL,
	[IpfsHash] [nvarchar](46) NULL,
	[Grade] [decimal](5, 2) NULL,
	[GradedAt] [datetime2](7) NULL,
	[IsPlagiarized] [bit] NULL,
	[SubmittedAt] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Certificates]    Script Date: 5/15/2025 6:08:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Certificates](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[CourseId] [uniqueidentifier] NOT NULL,
	[NftId] [nvarchar](66) NULL,
	[IpfsHash] [nvarchar](46) NULL,
	[IssuedAt] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Courses]    Script Date: 5/15/2025 6:08:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Courses](
	[Id] [uniqueidentifier] NOT NULL,
	[Title] [nvarchar](255) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime2](7) NULL,
	[UpdatedAt] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Enrollments]    Script Date: 5/15/2025 6:08:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Enrollments](
	[Id] [uniqueidentifier] NOT NULL,
	[UserId] [uniqueidentifier] NOT NULL,
	[CourseId] [uniqueidentifier] NOT NULL,
	[EnrolledAt] [datetime2](7) NULL,
	[Status] [nvarchar](50) NOT NULL,
	[ProgressPercentage] [decimal](5, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UK_Enrollments] UNIQUE NONCLUSTERED 
(
	[UserId] ASC,
	[CourseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 5/15/2025 6:08:50 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [uniqueidentifier] NOT NULL,
	[Email] [nvarchar](255) NOT NULL,
	[PasswordHash] [nvarchar](255) NOT NULL,
	[FullName] [nvarchar](100) NOT NULL,
	[Role] [nvarchar](50) NOT NULL,
	[Did] [nvarchar](255) NULL,
	[CreatedAt] [datetime2](7) NULL,
	[UpdatedAt] [datetime2](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Assignments] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Assignments] ADD  DEFAULT ((0)) FOR [IsPlagiarized]
GO
ALTER TABLE [dbo].[Assignments] ADD  DEFAULT (getutcdate()) FOR [SubmittedAt]
GO
ALTER TABLE [dbo].[Certificates] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Certificates] ADD  DEFAULT (getutcdate()) FOR [IssuedAt]
GO
ALTER TABLE [dbo].[Courses] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Courses] ADD  DEFAULT (getutcdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Enrollments] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Enrollments] ADD  DEFAULT (getutcdate()) FOR [EnrolledAt]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT (getutcdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Assignments]  WITH CHECK ADD  CONSTRAINT [FK_Assignments_Courses] FOREIGN KEY([CourseId])
REFERENCES [dbo].[Courses] ([Id])
GO
ALTER TABLE [dbo].[Assignments] CHECK CONSTRAINT [FK_Assignments_Courses]
GO
ALTER TABLE [dbo].[Assignments]  WITH CHECK ADD  CONSTRAINT [FK_Assignments_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Assignments] CHECK CONSTRAINT [FK_Assignments_Users]
GO
ALTER TABLE [dbo].[Certificates]  WITH CHECK ADD  CONSTRAINT [FK_Certificates_Courses] FOREIGN KEY([CourseId])
REFERENCES [dbo].[Courses] ([Id])
GO
ALTER TABLE [dbo].[Certificates] CHECK CONSTRAINT [FK_Certificates_Courses]
GO
ALTER TABLE [dbo].[Certificates]  WITH CHECK ADD  CONSTRAINT [FK_Certificates_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Certificates] CHECK CONSTRAINT [FK_Certificates_Users]
GO
ALTER TABLE [dbo].[Courses]  WITH CHECK ADD  CONSTRAINT [FK_Courses_Users] FOREIGN KEY([CreatedBy])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Courses] CHECK CONSTRAINT [FK_Courses_Users]
GO
ALTER TABLE [dbo].[Enrollments]  WITH CHECK ADD  CONSTRAINT [FK_Enrollments_Courses] FOREIGN KEY([CourseId])
REFERENCES [dbo].[Courses] ([Id])
GO
ALTER TABLE [dbo].[Enrollments] CHECK CONSTRAINT [FK_Enrollments_Courses]
GO
ALTER TABLE [dbo].[Enrollments]  WITH CHECK ADD  CONSTRAINT [FK_Enrollments_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Enrollments] CHECK CONSTRAINT [FK_Enrollments_Users]
GO
ALTER TABLE [dbo].[Enrollments]  WITH CHECK ADD CHECK  (([Status]='Completed' OR [Status]='InProgress' OR [Status]='Enrolled'))
GO
ALTER TABLE [dbo].[Users]  WITH CHECK ADD CHECK  (([Role]='Admin' OR [Role]='Student'))
GO
