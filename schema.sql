CREATE TABLE [dbo].[BeatSaberSong](
    [BeatSaverKey] [int] NOT NULL,
    [Hash] [binary](20) NOT NULL,
    [Uploader] [nvarchar](4000) NOT NULL,
    [Uploaded] [datetime2] NOT NULL,
    [Difficulties] [int] NOT NULL,
    [Bpm] [float] NOT NULL,
    [LevelAuthorName] [nvarchar](4000) NOT NULL,
    [SongAuthorName] [nvarchar](4000) NOT NULL,
    [SongName] [nvarchar](4000) NOT NULL,
    [SongSubName] [nvarchar](4000) NOT NULL,
    [Name] [nvarchar](4000) NOT NULL,
    [AutoMapper] [bit] NOT NULL,
    [CreatedAt] datetime2(7) NULL,
    [UpdatedAt] datetime2(7) NULL,
    [LastPublishedAt] datetime2(7) NULL,
    [DeletedAt] datetime2(7) NULL
 CONSTRAINT [PK_BeatSaberSong] PRIMARY KEY CLUSTERED 
(
    [BeatSaverKey] ASC
)
) ON [PRIMARY]
GO

CREATE FULLTEXT CATALOG [BeatSaverCatalog] WITH ACCENT_SENSITIVITY = ON
AS DEFAULT
GO

CREATE FULLTEXT INDEX ON BeatSaberSong(LevelAuthorName, SongAuthorName, SongName, SongSubName, Name) KEY INDEX PK_BeatSaberSong
GO

CREATE INDEX IDX_BeatSaberSong_DeletedAt_AutoMapper_Difficulties ON BeatSaverMatcher.dbo.BeatSaberSong (DeletedAt, AutoMapper, Difficulties)
GO

BEGIN TRANSACTION
GO
CREATE TABLE dbo.BeatSaberSongRatings
	(
	BeatSaverKey int NOT NULL,
	Upvotes int NOT NULL,
	Downvotes int NOT NULL,
	Score float(53) NOT NULL,
	UpdatedAt datetime2(7) NOT NULL
	)  ON [PRIMARY]
GO
ALTER TABLE dbo.BeatSaberSongRatings ADD CONSTRAINT
	PK_BeatSaberSongRatings PRIMARY KEY CLUSTERED 
	(
	BeatSaverKey
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
COMMIT
