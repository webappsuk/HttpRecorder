# <img src="doc/images/logo.png" width="32" /> HttpRecorder for .Net

**_Note: This is a Work In Progress, check back soon for updates_**

**Status:** Build and runs, no NuGet yet, correctly saves and replays response.

## Features
At its core the library is designed to record `HttpResponseMessage` results for any given `HttpRequestMessage` into a binary LZW compressed 'Cassette' file.  This is primarily designed to support both the following use cases:
- Recording calls to an external API for replaying in test runs, allowing tests to be run offline and reliably/repeatably.
- Efficient caching and retrieval of responses to for a website (at either end).

Many of the libraries out there support one or the other, but fundamentally the problems are the same, and just require a better interface design.

Other core features I couldn't find elsewhere:
- Highly extensible, including allowing different backing stores.
- Fast, compact storage, that supports byte content not just strings.  For this I'm using the [MessagePack](https://msgpack.org/index.html) format (via [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp)) with recordings held in a compressed archive.
- Correctly instrument `HttpClientFactory` clients, and support custom `HttpMessageHandler`s
- Allowing request matching to be highly customisable (i.e. deciding which parts of a request to match on whilst ignoring other elements)
- Accurately rebuilding a `HttpResponseMessage` as it was initially returned (this is particularly releveant to the `HttpResponseMessage.Content` type which most libraries do not reproduce accurately)
- Supports recording of the `HttpResponseMessage.RequestMessage` (see [`RequestRecordMode`](#RequestRecordMode) and [RequestPlaybackMode](#RequestPlaybackMode))
- Support all content types. [WIP]
- Parameterisation of Request/Response, as well as allowing a real request to be recorded and played back this could allow for dummy recordings to be made with parameters to take from the request and place in the response. [WIP]
- Secret hiding.  This was the original driver for parameterisation and can actually be solved using it, the idea is that secrets would not be stored on the cassette (e.g. passwords, keys, etc.) and instead replaced with parameters. [WIP]
- Doesn't assume custom handlers don't mangle the response's request object [TODO]
- Support streaming of responses to disk, allowing for large disks and efficient memory usage - particularly useful for replaying large file downloads, etc. [TODO]

## Examples
The starting point for any use is to create a `WebApplications.HttpRecorder.Cassette`.  Cassettes can be kept for the lifetime of the application or a seasion, but ideally should be disposed once finished with to ensure the underlying store is dispoed (if owned by the cassette) and to dispose any internal locks.

Creating a Cassette can be as easy as:
```csharp
using (Cassette cassette = new Cassette())
{
    ...
}
``` 

This will create a cassette capable of holding multiple recordings in a single file, which will be placed alongside the calling method's source file with an extension '__cassette.hrc_'.

### HttpClient instrumentation
The easiest way to instrument a [`System.Net.HttpClient`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=netstandard-1.6) is to have a `Cassette` create one for you:
```csharp
using (Cassette cassette = new Cassette())
using (HttpClient client = cassette.GetClient())
{
    ...
}
``` 

At this point any messages you send and receive with the `client` will be recorded to the Cassette.  If the Cassette already exists and contains a matching response, then it will replay the response without hiting the real endpoint.

Alternatively you can retrieve an instrumented [`System.Net.HttpMessageHandler`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpmessagehandler?view=netstandard-1.6) from the Cassette:
```csharp
using (Cassette cassette = new Cassette())
using (HttpMessageHandler handler = cassette.GetHttpMessageHandler())
using (HttpClient client = new HttpClient(handler))
{
    ...
}
``` 

The `GetHttpMessageHandler` method supports passing in an inner [`System.Net.HttpMessageHandler`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpmessagehandler?view=netstandard-1.6) making it capable of instrumenting your own pipelines.

### Record anywhere
Ultimately, all the Cassette's instrumentation overloads, ultimately call the core `RecordAsync` method which can be used to record and playback any response.
```csharp
using (Cassette cassette = new Cassette())
{
    await cassette.RecordAsync(request, async (r, ct) => response, cancellationToken);
    
    // or if you have the request and response already
    await cassette.RecordAsync(request, response, cancellationToken);
    
    // or if you only have the response (uses response.RequestMessage)
    await cassette.RecordAsync(response, cancellationToken);    
}
```

### Caching responses in middleware

TODO

## Changing key matching
Under the hood the Recorder converts a `HttpRequestMessage` to a 22 character URI Safe (see [Section 2.3 RFC 3986](http://www.ietf.org/rfc/rfc3986.txt)) hash.  It does this by serializing the request to a `byte[]` and then running an MD5 hash over it (yes I know MD5 is 'insecure' but it is a fast hash and we're not using it for security but collision avoidance which it is more than adequate for!).

The key data is generated using a `KeyGenerator`, the default one being `FullRequestKeyGenerator.Instance`.

TODO: Complete explanation

## Changing the backing store
The recorder reads and writes to a backing store, by default this is a single Zip Archive, however there are other options available.

TODO

## Options
The `Cassette` accepts a `CassetteOptions` object on creation which supply default options for all recordings made to the cassette.  It can also be overloaded when using `GetClient` or `GetHttpMessageHandler` to apply to recordings made by the client, and again on each individual call to `RecordAsync`. Options are applied over the top of the default options (`CassetteOptions.Default`).

You can create a `CassetteOptions` object using its constructor, for example:
```csharp
using (Cassette cassette = new Cassette(defaultOptions: new CassetteOptions(
    // These are the default options anyway, so no need to do this
    mode: RecordMode.Auto,
    waitForSave: false,
    simulateDelay: TimeSpan.Zero,
    requestRecordMode: RequestRecordMode.Ignore,
    requestPlaybackMode: RequestPlaybackMode.Auto)))
{
    using (HttpClient client = cassette.GetClient(new CassetteOptions(
        // Force this client to overwrite recordings and wait for saves
        mode: RecordMode.Overwrite,
        waitForSave: true)))
    {
        ...
    }
    using (HttpClient client = cassette.GetClient(new CassetteOptions(
        // This client will only playback (or error if no matching requests are found), and will use the recorded delay.
        mode: RecordMode.Playback,
        simulateDelay: TimeSpan.MinValue)))
    {
        ...
    }
}
```

You can also use some of the helper options and combine them using the `&` operator, for example the following is functionally equivalent to the above example (though there are more object allocations, so this syntax is best used in places it will not be run frequently):
```csharp
using (Cassette cassette = new Cassette(defaultOptions: CassetteOptions.Default))
{
    using (HttpClient client = cassette.GetClient(
        CassetteOptions.Overwrite & CassetteOptions.WaitUntilSaved))
    {
        ...
    }
    using (HttpClient client = cassette.GetClient(
        CassetteOptions.Playback & CassetteOptions.RecordedDelay))
    {
        ...
    }
}
```

### RecordMode
The most useful is the `RecordMode` option which controls how system handles recordings:

| Option        | Recording Found? | Outcome |  Action |
|---------------|:----------------:|:-------:|---------|
| **Default**   | :white_check_mark:/:negative_squared_cross_mark:| | _Will use the default option for the cassette_ |
| **Auto**      | :negative_squared_cross_mark: | :record_button: | Will hit the endpoint and record the response (default) |
| **Auto**      | :white_check_mark: | :play_or_pause_button: | Will playback the response, without hitting the endpoint (default) |
| **Playback**  | :negative_squared_cross_mark: | :exclamation: | _Throws a CassetteNotFoundException Exception_ |
| **Playback**  | :white_check_mark: | :play_or_pause_button: | Will playback the response, without hitting the endpoint |
| **Record**    | :negative_squared_cross_mark: | :record_button: | Will hit the endpoint and record the response |
| **Record**    | :white_check_mark: | :left_right_arrow: | Will hit the endpoint and _not_ record the response |
| **Overwrite** | :white_check_mark:/:negative_squared_cross_mark: | :record_button: | Will hit the endpoint and record the response |
| **None**      | :white_check_mark:/:negative_squared_cross_mark: | :left_right_arrow: | Will hit the endpoint and _not_ record the response |

### WaitForSave
The `WaitForSave` option when `true` will force the recorder to wait until the underlying store successfully saves the recording before returning, this will also allow for any store errors to be caught by the caller, otherwise they are just logged.  The default is to not wait, allowing for asychronous recording storage.

### SimulateDelay
The `SimulateDelay` option allows the recorder to simulate a delay in retrieving a response.  When the value is positive it will wait for the specified time before returning a response.  If the value is negative (e.g. `TimeSpan.MinValue`) then the recorder will use the recorded duration from the original request.  If the value is `TimeSpan.Zero` (or `default(TimeSpan)`) then no delay will be introduced and the playback will proceed as quickly as possible; this is the default setting.

### RequestRecordMode
The `RequestRecordMode` option is designed for more robust handling of the `HttpResponseMessage.RequestMessage`.  Normally this property should be equal to the original request as passed in, however there is no reason a custom `HttpMessageHandler` cant change or mangle the request object.  To facilitate this following modes are allowed:

| Mode | Meaning |
|------|---------|
| **Ignore** | The request is not recorded, nor is it checked for being changed, this makes for more compact and faster recording. (_Default_) |
| **RecordIfChanged** | The request is serialized prior to using the `HttpMessageHandler` and then serialized again from `HttpResponseMessage.RequestMessage` the two are compared and if identical the request is not stored. This makes recording slower as it will perform at least one additional serialization (and potentially two when using a non-default full key generator), and will compare the two serialized forms. |
| **AlwaysRecord** | The `HttpResponseMessage.RequestMessage` is always serialized and recorded, this is particularly useful if you know that it normally changes and avoids a comparison and potentially an extra serialization. |

_**Note:** The `RequestRecordMode` option doesn't effect playback in any way._

### RequestPlaybackMode
The `RequestPlaybackMode` option is designed to complement the `RequestRecordMode` by determining what the recorded should do when it finds a request in a recording:

| Mode | Meaning |
|------|---------|
| **Auto** | If a request is found in a recording it will be deserialized and the `HttpResponseMessage.RequestMessage` will be set to the recorded value. (_Default_) |
| **IgnoreRecorded** | The `HttpResponseMessage.RequestMessage` will always be set to the request initially passed in, any recorded request will be ignored. |
| **UseRecorded** | The `HttpResponseMessage.RequestMessage` will be set to the recorded request, if a recording is found, without a request, a `CassetteException` is thrown. |

## And finally...
### Credits
This project was inspired by [Scotch](https://github.com/mleech/scotch) and [VCR-Sharp](https://github.com/shiftkey/vcr-sharp), which in turn were inspired by [VCR](https://github.com/vcr/vcr).  Whilst this is entirely new work, I first tried Scotch and evaluated other options before trying for a fundamentally different design.

### TODOs

- Unit tests
- KeyGeneratorResolver isn't really used, review (was originally to let a cassette file find it's own KeyGenerator without it needing to be supplied, is this really a useful use case?)
- Complete Content serializers to respect underlying type.
- Support serialization of exceptions thrown during response retrieval.
- Complete parameterisation?
- Add NuGet build & deploy, including source link (see [Hanselman](https://www.hanselman.com/blog/ExploringNETCoresSourceLinkSteppingIntoTheSourceCodeOfNuGetPackagesYouDontOwn.aspx))
- Add CI build and test
- Add Tags to top of README for NuGet and CI Build
- See https://www.inheritdoc.io/ for `<inheritdoc />` support.
- For example's use [JSONPlaceHolder](https://github.com/typicode/jsonplaceholder).
