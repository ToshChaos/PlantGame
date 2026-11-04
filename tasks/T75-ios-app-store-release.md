# T75 — App Store listing, review and release

**Owner:** A8 Tester/Release · **Status:** TODO (see `TASKS-IOS.md`) · **Depends on:** T74

Most of this is App Store Connect forms, so a person submits; agents prepare the text and screenshots. Check the required slots and forms in App Store Connect itself, since they change.

## Work
- **Listing:** extend `docs/store-listing.md` with the Apple fields: name (30 characters), subtitle (30), keywords (100), promotional text, description, category, support URL (required) and privacy policy URL. Its asset list names Play assets only; keep both sets.
- **Screenshots:** iPhone 6.9" (1320 x 2868) or 6.5", and iPad 13" (2064 x 2752 or 2048 x 2732), in landscape. Captured from the Simulator through `mcp__Claude_Code_iOS_Simulator__control`, with real gameplay and no debug UI. Optional app preview video later.
- **Forms:** App Privacy (expected "Data Not Collected", from T73), the current age rating questionnaire, content rights (third-party assets), price free, regions. For the EU, the Digital Services Act trader status: a trader's contact details are shown publicly, so read this before answering.
- **Review notes:** offline, no login, no purchases, how to reach the first bridge. Test on the iPad too, because review does.
- **Release:** manual release after approval, so it can go out together with Android if wanted. Log it in `docs/private/publishing.md`, tag the commit.
- **After release:** a quick install from the store on a clean device; plan for the next update's build number.

## Acceptance
- The app is approved and live in the chosen regions.
- A clean install from the App Store plays Level 1, and a shared seed code works.

## Handoff
