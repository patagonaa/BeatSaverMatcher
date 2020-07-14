CREATE TABLE [dbo].[BeatSaberSong](
    [BeatSaverKey] [int] NOT NULL,
    [Hash] [binary](20) NOT NULL,
    [Uploader] [nvarchar](4000) NOT NULL,
    [Uploaded] [datetime2] NOT NULL,
    [Difficulties] [int] NOT NULL,
    [Bpm] [float] NOT NULL,
    [TextSearchValue] [nvarchar](4000) NOT NULL,
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