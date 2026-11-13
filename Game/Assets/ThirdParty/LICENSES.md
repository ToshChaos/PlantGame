# Third-party assets

Every sourced file (model, texture, sound, font, package outside Unity's registry) gets a row here.
No unlicensed assets (AGENTS.md rule 8).

| Asset | Where used | Author | License | URL |
|-------|-----------|--------|---------|-----|
| Quaternius Stylized Nature MegaKit (Standard, 19 models: CommonTree_1-5, TwistedTree_1-5, Pine_1-5, Bush_Common, Bush_Common_Flowers, Fern_1, Mushroom_Common) + its textures | `Art/Models/Quaternius/Nature`, `Art/Materials/Quaternius`; island trees, bushes, ferns, mushrooms (`Scenery.cs`) | Quaternius (quaternius.com) | CC0 1.0 (no credit required; credited in the Credits screen anyway) | https://quaternius.itch.io/stylized-nature-megakit |
| FreeStylized Ruins Modular Kit 01 (17 FBX pieces: AP4_A1_StoneWall1/3/5, AP4_A2_Stone1/2/3/5/6, AP4_A2_StoneWall2/4/6, AP4_A3_Door1, DoorStone1/2, LongStone1/2, StoneRing1; 3 baked atlases, base colour + GL normal, capped at 1024 px on import) | `Art/Models/Ruins`, `Art/Materials/Ruins`; portal arch frame, ruin walls and rubble, sky debris (`Scenery.cs`) | FreeStylized (freestylized.com) | Royalty Free: free for commercial and non-commercial use (custom licence, no credit required; credited anyway). Do not redistribute the raw files. | https://freestylized.com/asset_pack/ruins_01/ (download via the linked Patreon post) |

Only the models the game places are kept in `Game/Assets` (cleanups 2026-10-26 and 2026-11-13); the full packs stay in the git-ignored `Source/ThirdParty-raw/`.
The Kenney Nature Kit and Fantasy Town Kit (CC0) were used until 2026-11-13; their models are replaced by props built in code (`OrganicProps`) and no Kenney file is in the project.
