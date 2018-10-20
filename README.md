# <img src="doc/images/logo.png" width="32" /> HttpRecorder for .Net

**_Note: This is a Work In Progress, check back soon for updates_**

## Features
At its core the library is designed to record `HttpResponseMessage` results for any given `HttpRequestMessage` into a binary LZW compressed 'Cassette' file.  This is primarily designed to support both the following use cases:
- Recording calls to an external API for replaying in test runs, allowing tests to be run offline and reliably/repeatably.
- Efficient caching web server responses to disk.
- Fast, compact storage, that supports byte content not just strings.  For this I'm using the excellent [MessagePack](https://github.com/neuecc/MessagePack-CSharp) format with added compression.
- Correctly instrument `HttpClientFactory` clients, and support custom `HttpMessageHandler`s

Many of the libraries out there support one or the other, but fundamentally the problems are the same, and just require a better interface design.

Other core features I couldn't find elsewhere:
- Allowing request matching to be highly customisable (i.e. deciding which parts of a request to match on whilst ignoring other elements)
- Accurately rebuilding a `HttpResponseMessage` as it was initially returned (this is particularly releveant to the `HttpResponseMessage.Content` type which most libraries do not reproduce accurately)
- Support streaming of responses to disk, allowing for large disks and efficient memory usage - particularly useful for replaying large file downloads, etc. [TODO]
- Support all content types.
- Parameterisation of Request/Response, as well as allowing a real request to be recorded and played back this could allow for dummy recordings to be made with parameters to take from the request and place in the response. [WIP]
- Secret hiding.  This was the original driver for parameterisation and can actually be solved using it, the idea is that secrets would not be stored on the cassette (e.g. passwords, keys, etc.) and instead replaced with parameters. [WIP]
- Doesn't assume custom handlers don't mangle the response's request object [WIP]

## Examples

# HttpClient instrumentation

## And finally...
### Credits
This project was inspired by [Scotch](https://github.com/mleech/scotch) and [VCR-Sharp](https://github.com/shiftkey/vcr-sharp), which in turn were inspired by [VCR](https://github.com/vcr/vcr).  Whilst this is entirely new work, playing with those libraries inspired me to write this version.

### TODOs

- Add NuGet build & deploy
- Add CI build and test
- Add Tags to top of README for NuGet and CI Build
- See https://www.inheritdoc.io/ for `<inheritdoc />` support.
- For example's use [JSONPlaceHolder](https://github.com/typicode/jsonplaceholder).
