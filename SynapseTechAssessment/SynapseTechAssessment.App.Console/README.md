# Dev Notes

## Tools Used
- Rider
- JetBrains AI Assistant (Claude Agent)
  - Used Claude to add startup code (logging, config, etc.)
  - Used Claude to generate the majority of code for OrderClient, FileReader, and PhysicianNoteExtractor classes
- Google

## Future Enhancement Reccomendations
- FileReader
  - Stream file contents using IAsyncEnumerable so we can process each file in parallel
  - Update to return file information along with the file contents for improved logging
- PhysicianNoteExtractor
  - Validate the Order object after extraction