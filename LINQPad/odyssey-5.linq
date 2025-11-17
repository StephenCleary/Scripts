<Query Kind="Expression" />

/*
episode_number (string)
long_name - parse for title
devotional (html)
description
long_theme
bible_verse
author, authors
characters
air_date
tags[].name

Artist = "Various Artists"
Album Title = "Adventures in Odyssey"
Song Title = parsed long_name
Track Number = episode_number
Original Date = air_date
TODO: devotional, description, long_theme, bible_verse, author, authors, characters, tags
Alternative: actually order by album.

== Plex recognizes:
Artist: only the first artist if there are multiple
Album Artist: only the first artist
Album Title
Song Title
Track Number
Disc Number
Record Label/Publisher
Release Type
Original Date, (Recording) Date, Release Date: Plex reads all three tags, but only one of these is used, in this order, i.e. if you’ve tagged 1960, 1995 and 2015 for these fields, Plex will show the album as 1960. Only one date per album, unfortunately no individual dates for individual songs.
Genre: multiple values, but only the Genres of the first song of the album: so if track 1 has genres “Rock”, “Pop” tagged and track 2 has “House”, then all songs on the album will have two genres “Rock” and “Pop” in Plex.
*/