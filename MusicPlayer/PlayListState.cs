using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;

namespace MusicPlayer
{
    [DataContract]
    internal class PlayListCollectionState
    {
        public PlayListCollectionState(IEnumerable<PlaylistState> playlists)
        {
            this.playlists = playlists.ToList();
        }

        [DataMember]
        private readonly List<PlaylistState> playlists;
        [IgnoreDataMember]
        public List<PlaylistState> Playlists => this.playlists is null ? new List<PlaylistState>() : this.playlists;
        public static PlayListCollectionState Empty => new PlayListCollectionState(Array.Empty<PlaylistState>());

        public static PlayListCollectionState LoadFromString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return Empty;
            var serelizer = new DataContractSerializer(typeof(PlayListCollectionState));
            using (var stringReader = new StringReader(str))
            using (var xmlReader = XmlReader.Create(stringReader))
            {

                var @object = serelizer.ReadObject(xmlReader);
                return (PlayListCollectionState)@object;
            }
        }

        public string Persist()
        {
            var serelizer = new DataContractSerializer(typeof(PlayListCollectionState));
            using (var stringWriter = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter))
            {
                serelizer.WriteObject(xmlWriter, this);
                xmlWriter.Flush();

                return stringWriter.ToString();
            }
        }


        public IEnumerable<Change> GetChanges(PlayListCollectionState newPlaylists)
        {

            var createdPlaylists = newPlaylists.Playlists.Except(this.Playlists).ToArray();
            var deletedPlaylists = this.Playlists.Except(newPlaylists.Playlists).ToArray();

            foreach (var item in deletedPlaylists)
                yield return new PlayListDeletedChange() { PlaylistId = item.Id };

            foreach (var item in createdPlaylists)
                yield return new PlayListCreatedChange()
                {
                    PlaylistId = item.Id,
                    Name = item.Name,
                    Songs = item.Songs
                };


            foreach (var maybeChanged in this.Playlists.Intersect(newPlaylists.Playlists))
            {
                var otherP = newPlaylists.Playlists.First(x => x.Id == maybeChanged.Id);



                if (maybeChanged.Name != otherP.Name)
                {
                    yield return new PlayListNameChange()
                    {
                        PlaylistId = maybeChanged.Id,
                        NewName = otherP.Name
                    };
                }

                var deletedSongs = maybeChanged.Songs.Except(otherP.Songs).ToArray();
                var createdSongs = otherP.Songs.Except(maybeChanged.Songs).ToArray();

                foreach (var item in deletedSongs)
                    yield return new SongDeletedChange()
                    {
                        PlaylistId = maybeChanged.Id,
                        LibraryProvider = item.LibraryProvider,
                        MediaId = item.MediaId
                    };

                foreach (var item in createdSongs)
                    yield return new SongCreatedChange()
                    {
                        PlaylistId = maybeChanged.Id,
                        LibraryProvider = item.LibraryProvider,
                        MediaId = item.MediaId,
                        Index = otherP.Songs.IndexOf(item)
                    };

                foreach (var item in maybeChanged.Songs.Intersect(otherP.Songs))
                {
                    var oldIndex = maybeChanged.Songs.IndexOf(item);
                    var newIndex = otherP.Songs.IndexOf(item);

                    if (oldIndex != newIndex)
                        yield return new SongIndexChange()
                        {
                            PlaylistId = maybeChanged.Id,
                            LibraryProvider = item.LibraryProvider,
                            MediaId = item.MediaId,
                            Index = newIndex
                        };
                }

            }




        }
    }

    abstract class Change
    {

    }

    abstract class PlayListChange : Change
    {
        public Guid PlaylistId { get; set; }
    }

    abstract class SongChange : PlayListChange
    {
        public string MediaId { get; set; }
        public string LibraryProvider { get; set; }

    }

    class SongDeletedChange : SongChange { }

    class SongCreatedChange : SongChange
    {
        public int Index { get; set; }

    }

    class SongIndexChange : SongChange
    {
        public int Index { get; set; }

    }


    class PlayListNameChange : PlayListChange
    {
        public string NewName { get; set; }
    }

    class PlayListDeletedChange : PlayListChange { }
    class PlayListCreatedChange : PlayListChange
    {
        public string Name { get; set; }

        public List<SongState> Songs { get; set; }
    }


    [DataContract]
    class PlaylistState : IEquatable<PlaylistState>
    {
        public PlaylistState(Guid id, string name, IEnumerable<SongState> songs)
        {
            this.Id = id;
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.songs = songs.ToList();
        }

        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        private readonly List<SongState> songs;
        [IgnoreDataMember]
        public List<SongState> Songs => this.songs is null ? new List<SongState>() : this.songs;

        public override bool Equals(object obj)
        {
            return obj is PlaylistState state && this.Equals(state);
        }

        public bool Equals(PlaylistState other)
        {
            return this.Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return 2108858624 + EqualityComparer<Guid>.Default.GetHashCode(this.Id);
        }

        public static bool operator ==(PlaylistState left, PlaylistState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlaylistState left, PlaylistState right)
        {
            return !(left == right);
        }
    }

    [DataContract]
    class SongState : IEquatable<SongState>
    {
        public SongState(string libraryProvider, string mediaId)
        {
            this.LibraryProvider = libraryProvider ?? throw new ArgumentNullException(nameof(libraryProvider));
            this.MediaId = mediaId ?? throw new ArgumentNullException(nameof(mediaId));
        }

        [DataMember]
        public string LibraryProvider { get; set; }
        [DataMember]
        public string MediaId { get; set; }

        public override bool Equals(object obj)
        {
            return obj is SongState state && this.Equals(state);
        }

        public bool Equals(SongState other)
        {
            return this.LibraryProvider == other.LibraryProvider &&
                   this.MediaId == other.MediaId;
        }

        public override int GetHashCode()
        {
            var hashCode = -1793927485;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.LibraryProvider);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.MediaId);
            return hashCode;
        }

        public static bool operator ==(SongState left, SongState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SongState left, SongState right)
        {
            return !(left == right);
        }
    }
}