# WP10-Music-Player

Since  OneDrive no longer Supports streaming, and Groove killed downloading of music and syncing playlistst the same time,
this is an alternative for Windows Mobile and Win 10. It is planed to download Your Music from OneDrive and Syncing your Playlists.

It will need the latest Windows 10 Mobile Version. It ist Testet on an Lumia 950 DualSim and Win 10 1903.


![Mockup](description/Mockup.png)  
*The mockup used to creates this picture was provided by [GRAPHBERRY](https://www.graphberry.com/item/web-screens-psd-mockup)*

Currently supported
-------------------

 - List changes on Ondrive and seperatly download them (you maybe don't want to sync giga bytes on a metered connection)
 - Sync of Playlist
 - Play Music (incl. shuffle and repat all)
 - Responsive UI
 
 
 ToDo
 ----
 - [ ] Option to scan your music for corupted files and redownload them. Currently when somthing corrupts a file, the only thing you can do is delete everything and sync from scratch :(
 - [x] ~~Get Release build to work. The UWP Native compiler has some issues. But this would be nessesaary to get it in the store. (And could result in better performance)~~
 - [ ] Sync only selected music from onedrive (currently it is all or nothing)
 - [ ] More Playlist edit options. Currently you can only add songs to an playlist and create new playlists. You can't ~~delete Playlists or~~ remove songs. (The UI is missing for this)
 - [ ] Save the current playing list between starts (propably using project rome to sync accross devices and put it in the windows timeline)
 - [ ] Performance improvements (esspecially for Phone)
 - [ ] Live Tile
 - [x] ~~A nice full screen Now Playing view. (Something you could show on your TV so everyone sees what's now playing)~~
 - [ ] It would also be nice to support different provider besides OneDrive. Storage of information is already prepared for this. But it is not high on the TODO list.
 - [ ] Creating variants for Xamarin (and maybe Blazor)
 
