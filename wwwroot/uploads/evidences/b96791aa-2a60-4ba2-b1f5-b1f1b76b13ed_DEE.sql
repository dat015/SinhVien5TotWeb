create database DEE_System
use DEE_System

CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(50) NOT NULL CHECK (Role IN ('Student', 'Admin')),
    Did NVARCHAR(255), -- Tham chiếu DID trên Polygon (did:polygon:...)
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2
);
CREATE TABLE Courses (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Title NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2,
    CONSTRAINT FK_Courses_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
);

CREATE TABLE Enrollments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    CourseId UNIQUEIDENTIFIER NOT NULL,
    EnrolledAt DATETIME2 DEFAULT GETUTCDATE(),
    Status NVARCHAR(50) NOT NULL CHECK (Status IN ('Enrolled', 'InProgress', 'Completed')),
    ProgressPercentage DECIMAL(5,2), -- Tiến trình học (0-100%)
    CONSTRAINT FK_Enrollments_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Enrollments_Courses FOREIGN KEY (CourseId) REFERENCES Courses(Id),
    CONSTRAINT UK_Enrollments UNIQUE (UserId, CourseId)
);

CREATE TABLE Certificates (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    CourseId UNIQUEIDENTIFIER NOT NULL,
    NftId NVARCHAR(66), -- Transaction hash hoặc ID của NFT trên Polygon
    IpfsHash NVARCHAR(46), -- CID của metadata NFT trên IPFS
    IssuedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Certificates_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Certificates_Courses FOREIGN KEY (CourseId) REFERENCES Courses(Id)
);

CREATE TABLE Assignments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    CourseId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    Content NVARCHAR(MAX), -- Nội dung bài tập (văn bản, mã nguồn, v.v.)
    IpfsHash NVARCHAR(46), -- CID của bài tập trên IPFS (nếu là tệp lớn)
    Grade DECIMAL(5,2), -- Điểm số (0-100)
    GradedAt DATETIME2,
    IsPlagiarized BIT DEFAULT 0, -- Kết quả kiểm tra đạo văn
    SubmittedAt DATETIME2 DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Assignments_Users FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Assignments_Courses FOREIGN KEY (CourseId) REFERENCES Courses(Id)
);