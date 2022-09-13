import * as ko from "knockout";
import 'bootstrap/dist/css/bootstrap.min.css';
import './style.css';

class AppViewModel {
    public playlistId = ko.observable<string>();
    public playlistUri = ko.observable<string>('');
    public preferredDifficulties = ko.observableArray<string>(['1', '2', '4', '8', '16']);
    public preferredDifficultiesFlags = ko.computed(() => this.preferredDifficulties().map(x => +x).reduce((a, b) => a | b, 0));
    public stateName = ko.observable<string>('None');
    public workItem = ko.observable<WorkResultItem>();
    public result = ko.observable<SongMatchResult>();

    public async run() {
        let matches = this.playlistId().match(/(?:playlist[\/:])?([\w]+)(?:\?.+)?$/);

        if (matches) {
            this.playlistId(matches[1]);
        }

        await fetch(`/api/Matches/${this.playlistId()}`, {
            method: 'POST'
        });

        var result: SongMatchResult;
        var item: WorkResultItem = null;
        while (result == null) {
            try {
                var response = await fetch(`/api/Matches/${this.playlistId()}`);
                item = <WorkResultItem>await response.json();
            } catch (e) {
                item.state = SongMatchState.Error;
            }

            this.workItem(item);
            if (item.state == SongMatchState.Error) {
                this.stateName(SongMatchState[item.state]);
                throw 'Something went wrong!';
            }

            if (item.state == SongMatchState.Finished) {
                result = item.result;
                break;
            }
            this.stateName(SongMatchState[item.state]);
            await new Promise(r => setTimeout(r, 1000));
        }

        for (let match of result.matches) {
            match.beatMaps.forEach(x => x.selected = ko.observable(false));

            let firstPreferredDifficulty = match.beatMaps.find(x => (x.difficulties & this.preferredDifficultiesFlags()) > 0);
            if (firstPreferredDifficulty != null)
                firstPreferredDifficulty.selected(true);

            match.beatMaps.forEach(x => x.selected.subscribe(() => this.updatePlaylistUri()));
        }
        this.result(result);
        this.updatePlaylistUri();
        this.stateName(SongMatchState[item.state]);
    }

    public getDifficulties(difficulties: number) {
        var toReturn = [];
        if (difficulties & 1)
            toReturn.push('Easy');
        if (difficulties & 2)
            toReturn.push('Normal');
        if (difficulties & 4)
            toReturn.push('Hard');
        if (difficulties & 8)
            toReturn.push('Expert');
        if (difficulties & 16)
            toReturn.push('ExpertPlus');
        return toReturn;
    }

    private updatePlaylistUri() {
        let keys: string[] = [];

        for (let match of this.result().matches) {
            for (let beatSaberMatch of match.beatMaps) {
                if (beatSaberMatch.selected())
                    keys.push(beatSaberMatch.beatSaverKey.toString(16));
            }
        }
        var uri = `${window.location.protocol}//${window.location.host}/api/ModSaberPlaylist/${keys.join(',')}/${this.playlistId()}.bplist`;

        this.playlistUri(uri);
    }
}

interface WorkResultItem {
    playlistId: string;
    state: SongMatchState;
    result: SongMatchResult;
    itemsProcessed: number;
    itemsTotal: number;
}

enum SongMatchState {
    None,
    Waiting,
    LoadingSpotifySongs,
    SearchingBeatMaps,
    LoadingBeatMapRatings,
    Finished,
    Error
}

interface SongMatchResult {
    matchedSpotifySongs: number;
    totalSpotifySongs: number;
    matches: SongMatch[];
}

interface SongMatch {
    spotifyArtist: string;
    spotifyTitle: string;
    beatMaps: BeatSaberSong[];
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
    rating: number;
    upVotes: number;
    downVotes: number;
}

function init() {
    ko.applyBindings(new AppViewModel(), document.getElementById("main"));
}

window.onload = init;