Domain Availability Checker A lightweight and efficient C# tool to check
the availability of domain names across multiple short top-level domains
(TLDs). This tool performs WHOIS queries to determine if a domain is
registered or available.

## Features

-   **Multi-TLD Support**: Checks availability across 200+ short TLDs (3
    characters or less)
-   **Concurrent Queries**: Performs parallel WHOIS queries with
    configurable concurrency limits
-   **Clean Results**: Clearly displays both registered and available
    domains
-   **Compact Output**: Focuses on shorter, more memorable domain names
-   **Standalone Executable**: Packaged as a single EXE file with no
    dependencies

## Supported TLDs

The tool checks availability for short TLDs including: - **Traditional
gTLDs**: .com, .net, .org, .info, .biz, .pro - **Country code TLDs**:
.us, .uk, .de, .fr, .it, .es, .nl, .ca, .au, .jp, .cn, .io, .co, .ai,
.tv - **New gTLDs**: .xyz, .top, .app, .dev, .art, .fit, .fun

And many more short TLDs (complete list in the source code)

## Usage

1.  Download the latest release from the Releases page
2.  Run `DomainName.exe`
3.  Enter the domain name (without extension) when prompted
4.  The tool will display real-time results as it checks each TLD
5.  After completion, it will list all available domains

### Example

    Enter root domain (without extension): example

    Checking example.com : Registered
    Checking example.net : Registered
    Checking example.org : Registered
    Checking example.io : Available
    Checking example.app : Available
    ...

    All available domains:
    example.io
    example.app
    example.dev
    ...

    Found 3 available domains

## Building from Source

### Prerequisites

-   .NET 6.0 SDK or later

### Compilation Steps

1.  Clone or download the source code
2.  Open a command prompt in the project directory
3.  Run the following command to build a standalone executable:

``` bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true
```

The compiled executable will be available in the
`bin/Release/net6.0/win-x64/publish/` directory.

### Build Options

-   Remove `-r win-x64` to target the current platform instead of
    specifically Windows x64
-   Remove `--self-contained true` to create a smaller executable that
    requires .NET Runtime installed
-   Remove `/p:PublishTrimmed=true` to disable code trimming (may
    increase size but improve compatibility)

## Technical Details

-   **Concurrency**: Limits to 5 simultaneous WHOIS queries to avoid
    being blocked
-   **WHOIS Servers**: Uses appropriate WHOIS servers for different TLDs
-   **Response Parsing**: Analyzes WHOIS responses for patterns
    indicating registration status
-   **Error Handling**: Gracefully handles network timeouts and WHOIS
    server errors

## Limitations

-   WHOIS queries may be rate-limited by some registries
-   Some TLDs may have special registration rules not reflected in
    availability checks
-   Response parsing may not work perfectly for all WHOIS server formats
-   The tool focuses on shorter TLDs (3 characters or less)

## Contributing

Contributions are welcome! Please feel free to submit pull requests
for: - Additional TLD support - Improved WHOIS response parsing -
Performance enhancements - Bug fixes

## License

This project is provided as-is without any warranty. You are free to use
and modify the code for personal or commercial purposes.

## Disclaimer

This tool is provided for educational and informational purposes only.
Domain availability results should be verified through official
registrars before making purchase decisions. The authors are not
responsible for any inaccuracies in domain availability information or
any consequences resulting from the use of this tool.
