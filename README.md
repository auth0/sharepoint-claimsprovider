Auth0 Claims Provider for SharePoint 2010

## Prerequisites
  - SharePoint solution development tools for Visual Studio 2010
  - Windows Identity Foundation | <a href="http://www.microsoft.com/en-us/download/details.aspx?id=17331" target="_blank">download</a>
  - NuGet Package Manager | <a href="http://visualstudiogallery.msdn.microsoft.com/27077b70-9dad-4c64-adcf-c7cf6bc9970c" target="_blank">download</a>

## Installation

  1. Open solution and enable "NuGet Package Restore"
  2. Compile solution
  2. Right click on project -> Package (that will generate a .wsp file)
  3. Install and deploy the solution from PowerShell:

  ~~~
  Add-SPSolution -LiteralPath "<path to .wsp file>"
  Install-SPSolution -Identity auth0.claimsprovider.wsp -GACDeployment
  ~~~

## Configuration

  1. Go to Central Admin -> Security
  2. Under General Security section, click on "Configure Auth0 Claims Provider"
  3. Set the required configuration parameters (like domain, client id and client secret)

## Documentation

For more information about <a href="http://auth0.com" target="_blank">auth0</a> visit our <a href="http://docs.auth0.com/" target="_blank">documentation page</a>.

## License

This client library is MIT licensed.
