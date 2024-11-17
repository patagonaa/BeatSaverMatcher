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
    [AutoMapper] [nvarchar](255) NULL,
 CONSTRAINT [PK_BeatSaberSong] PRIMARY KEY CLUSTERED 
(
    [BeatSaverKey] ASC
)
) ON [PRIMARY]
GO

CREATE FULLTEXT CATALOG [BeatSaverCatalog] WITH ACCENT_SENSITIVITY = ON
AS DEFAULT

CREATE FULLTEXT INDEX ON BeatSaberSong(LevelAuthorName, SongAuthorName, SongName, SongSubName, Name) KEY INDEX PK_BeatSaberSong

BEGIN TRANSACTION
GO
ALTER TABLE dbo.BeatSaberSong ADD
	CreatedAt datetime2(7) NULL,
	UpdatedAt datetime2(7) NULL,
	LastPublishedAt datetime2(7) NULL
GO
COMMIT