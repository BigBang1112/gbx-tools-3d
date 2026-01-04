# 3D Gbx Tools

3D Gbx Tools (codename: GbxTools3D) is a web browser-based tooling for analyzing replays, maps, and 3D objects from Nadeo games.

Written using the Blazor Web App framework.

## Use cases

- Playing on a TMF server and wanting to see someone's replay
- Seeing runs on a phone when there's no footage
- Icons and screenshots are not descriptive enough

## Features

- View replays and ghosts with checkpoints, input list, and ghost sample parameters
- Have an overview of a map with the new map viewer
- Visualize skins from ZIP files or ManiaPark (Pack.Gbx not yet supported)
- Catalog for an overview of block variants, collisions, and other technical parameters
- Embed the tools into your websites using Widgets
- TMF/TM2/TMT/SM/TMNESWC/TMSX support

## Build

Make sure to have **.NET 10** and **.NET WebAssembly Build Tools** installed to be able to build the project.

You also need a **MariaDB** or a MySQL database. The default development connection is available in the `appsettings.Development.json`, the database will be created automatically when running. You can setup the database service manually or using Docker with port publishing.

**For production deployment, secure the database together with the connection string.** You also need to manually apply the migration, via the `Script-Migration` command or bundles for example.

### Visual Studio 2022

If you have Visual Studio 2022, you can use the Docker Compose setup, which will automatically launch the app, database, phpMyAdmin, and Seq for development purposes in debug mode.

The IDE will handle the persistent environment for the database until Visual Studio is closed. This could be sometimes annoying as the dataset can take a few minutes to process and it would have to be ran each time you open the project.

### Dataset import

The source code provides no game assets by default intentionally, you have to import them yourself.

The import is designed to be safe and possible to run at any point without corrupting anything. There may only be unused data in the database as the result of it if larger changes occur.

There are two config variables that control this (you can set the exact same as environment variable):

- `DatasetPath` - specifies the path to the dataset folder
- `DatasetImportKey` - a **secret** that verifies that you are the one running the import

The dataset folder has a simple structure:

- `[GameVersion]` (ID is taken from `GBX.NET.GameVersion`)
  - *contents* of the GameData folder of `GameVersion`

for as many `GameVersion` as there is supported: TMSX, TMNESWC, TMF, TMT, MP4, TM2020. If the folder is not found, the import for this game version is skipped (an empty `GameVersion` folder cannot exist otherwise there will be an error).

To extract data from games:

- TMF - https://io.gbx.tools/pak-to-zip (for corrupted crucial Gbx files - hit me up on Discord)
- other - TBD (OpenPlanet or GBX.NET.PAK)

To run the import, call the endpoint:

```
POST /api/dataimport
X-Key: my_secret_key
```

in Visual Studio or JetBrains Rider, you can utilize the `WebApi.http` file and call it from there. The project also exposes Scalar in Development mode, so you can use that on `/scalar` endpoint.

The import doesn't log everything yet, so it may appear weird at first, but things should import as expected.

After import is complete, you should have a usable app ready.

## License

AGPL-3.0

## Do not use this to clone the game!

The physics are the best part of Trackmania, and you cannot replicate those, so nobody will also play your clone.
