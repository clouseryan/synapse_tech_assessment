# Dev Notes

## Tools Used
- Rider
- JetBrains AI Assistant (Claude Agent)
  - Used Claude to add startup code (logging, config, etc.)
  - Used Claude to generate the majority of code for OrderClient, FileReader, and PhysicianNoteExtractor classes
- Google

## Future Enhancement Recommendations
- FileReader
  - Stream file contents using IAsyncEnumerable so we can process each file in parallel
  - Update to return file information along with the file contents for improved logging
- PhysicianNoteExtractor
  - Validate the Order object after extraction
- OrderClient
  - Use polly to add resilience to the client
- PhysicianNotesFileWorker
  - Add better error handling/fault tolerance. Save failures to DB for retry/analysis, etc.
  - Refactor to consumer from a message queue

## Other notes
- In appsettings, change the OrderClient Bypass setting to true to simulate a successful response
- Currently cofigured to run against LMStudio, change OpenAiSettings in appsettings.json to point to openai api