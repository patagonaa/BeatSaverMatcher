import * as ko from "knockout";
import 'bootstrap/dist/css/bootstrap.min.css';
import './style.css';

class AppViewModel {
    public playlistId = ko.observable<string>();
    public playlistUri = ko.observable<string>('');
    public matches = ko.observableArray<SongMatch>();

    public async run() {
        var response = await fetch(`/api/Matches/${this.playlistId()}`);
        var matches = <SongMatch[]>await response.json();
        for (let match of matches) {
            for (let beatSaberMatch of match.matches) {
                beatSaberMatch.selected = ko.observable(true);
                beatSaberMatch.selected.subscribe(() => this.updatePlaylistUri());
            }
        }
        this.matches(matches);
        this.updatePlaylistUri();
    }

    private updatePlaylistUri() {
        var uri = `bsplaylist://playlist/${window.location.protocol}//${window.location.host}/api/ModSaberPlaylist/${this.playlistId()}`;

        let keys: string[] = [];

        for (let match of this.matches()) {
            for (let beatSaberMatch of match.matches) {
                if (beatSaberMatch.selected())
                    keys.push(beatSaberMatch.beatSaverKey.toString(16));
            }
        }

        uri += '/' + keys.join(',');

        this.playlistUri(uri);
    }
}

interface SongMatch {
    spotifyArtist: string;
    spotifyTitle: string;
    matches: BeatSaberSong[];
}

interface BeatSaberSong {
    levelAuthorName: string;
    songAuthorName: string;
    songName: string;
    songSubName: string;
    bpm: number;
    name: string;
    difficulties: number;
    uploader: string;
    hash: string;
    beatSaverKey: number;
    selected: KnockoutObservable<boolean>;
}

function init() {
    ko.applyBindings(new AppViewModel(), document.getElementById("main"));
}

window.onload = init;