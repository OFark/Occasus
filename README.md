# Occasus
*.net core IOptions manager and UI*

## *Breaking changes*
- Going from 7.0.x to 8.0.x requires .net 8
- Going to 8.1.x will change the encryption method, settings will be scrambled. Copy the settings out to clipboard/file and delete the encrypted settings from storage before upgrading.

## What is it?

At it's core Occasus is an easy way to move .Net Options from the appSettings.json file to another storage repository.

It also has an optional UI to allow editing of those values during runtime.

## What is supported?

Currently .Net core 8; Console Applications, Blazor Apps and API, and Azure Functions. 

If you have a dependancy injected IConfigurationBuilder and IServiceCollection you should be good to go.

## How do I get started?

First of all you need to pick one (or more) of the repository packages for Occasus, and possibly a UI package. See more below.

Then you can use them in your configuration:

### Start with an IConfigurationBuilder

```c#
var builder = WebApplication.CreateBuilder(args);

builder.UseSettingsFrom...(options)
```

Where the UseSettingsFrom... is an extension from a Occasus repository package

### Then add your options to the IServiceCollection

```c#
.WithOptions<MyOptionsClass>()
```
Note: Here the WithOptions is an extension to the repository added to the builder.

You can also refer back to the Configured Option, just like regular Option injecting, where you can use the OptionsBuilder:
```c#
.WithOptions<MyOptions>(out var optionsBuilder);
optionsBuilder.Validate(x => x.SomeRequiredValue != null && x.ListOfStrings.Any(), "MyOptions Strings must have some value");
```

So you can validate on startup here too. 


### Finally

You can inject the .Net [`IOptions<MyOptionsClass>`](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options) into your application. 

```c# 
private readonly MyOptionsClass myoptions;
public MyService(IOptionsSnapshot<MyOptionsClass> myoptions)
{
    this.myoptions = myoptions.Value;
}
```

You can read more in the link above but if your usinbg IOptionsSnapshot, this is a Scoped access and the value will be retrieved everytime you start a new scope. This means any changes in the UI should be reflected without having to restart the application.

### Example

So if you were getting settings from SQL using the Occasus SQLEFRepository and using the Blazor UI package
```c#
var builder = WebApplication.CreateBuilder(args);

builder.AddOccasusUI()

.UseOptionsFromSQLEF(settings =>
{
    settings.WithSQLConnection(sqlConnBuilder =>
    {
        sqlConnBuilder.ConnectionString = builder.Configuration["ConnectionStrings:SettingsConnectionString"];
    });
})
.WithOptions<ApplicationOptions>()
```

Where `ApplicationOptions` is a class or record for the options from your application.

## Occasus.JSONRepository

[![Nuget](https://img.shields.io/nuget/v/Occasus.JSONRepository)](https://www.nuget.org/packages/Occasus.JSONRepository/)

[![GitHub last commit](https://img.shields.io/github/last-commit/OFark/Occasus)](https://github.com/OFark/Occasus)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/OFark/Occasus/nuget.yml?branch=main)

```PowerShell
dotnet add package Occasus.JSONRepository
```

```PowerShell
Install-Package Occasus.JSONRepository
```

This allows you to use a .json file as a repository for settings, it also allows you to use the appSettings.json file.

Simple usage:

`UseOptionsFromJsonFile(string filePath)`
```c#
builder.UseOptionsFromJsonFile("settings/settings.json")
    .WithOptions<ApplicationOptions>();
```

Advanced JSON Options:

`UseOptionsFromJsonFile(string filePath, Action<JsonSourceSettings> jsonSourceSettings)`
```c#
builder.UseOptionsFromJsonFile("appsettings.json", settings =>
{
    settings.JsonWriterOptions((ref JsonWriterOptions options) => options.Indented = true);
    settings.JsonNodeOptions((ref JsonNodeOptions options) => options.PropertyNameCaseInsensitive = true);
})
    .WithOptions<TestAppSettingsJson>();
```

### JsonSourceSettings
JsonSourceSettings is a class that configures the repository:

- ClearWholeFile - Boolean flag, when writing to the file, start with a clean file, or leave other json Nodes alone. This options is useful during development when your application options may change.

Occasus.JsonRepository uses [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/api/system.text.json?view=net-7.0) and the following actions are passed directly to that:
- JsonDocumentOptions(ActionRef\<JsonDocumentOptions> options) - this is a ref for the JsonDocumentOptions configuration action. 
- JsonNodeOptions(ActionRef\<JsonNodeOptions> options) - this is a ref for the JsonNodeOptions configuration action. 
- JsonSerializerOptions(Action\<JsonSerializerOptions> options) - this is *not* a ref but it's an action for the JsonSerializerOptions
- JsonWriterOptions(ActionRef\<JsonWriterOptions> options) - this is a ref for the JsonWriterOptions configuration action.


## Occasus.SQLRepository & Occasus.SQLEFRepository

### Occasus.SQLRepository
[![Nuget](https://img.shields.io/nuget/v/Occasus.SQLRepository)](https://www.nuget.org/packages/Occasus.SQLRepository/)

[![GitHub last commit](https://img.shields.io/github/last-commit/OFark/Occasus)](https://github.com/OFark/Occasus)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/OFark/Occasus/nuget.yml?branch=main)

```PowerShell
dotnet add package Occasus.SQLRepository
```

```PowerShell
Install-Package Occasus.SQLRepository
```

### Occasus.SQLEFRepository
[![Nuget](https://img.shields.io/nuget/v/Occasus.SQLEFRepository)](https://www.nuget.org/packages/Occasus.SQLEFRepository/)

[![GitHub last commit](https://img.shields.io/github/last-commit/OFark/Occasus)](https://github.com/OFark/Occasus)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/OFark/Occasus/nuget.yml?branch=main)

```PowerShell
dotnet add package Occasus.SQLEFRepository
```

```PowerShell
Install-Package Occasus.SQLEFRepository
```

This allows you to use a SQL Server for settings.

Usage example:

```c#
builder.AddOccasusUI()

.UseOptionsFromSQL(settings =>
{
    settings.EncryptSettings = true;
    settings.EncryptionKey = "a password";
    settings.WithSQLConnection(sqlConnBuilder =>
    {
        sqlConnBuilder.ConnectionString = builder.Configuration["ConnectionStrings:SettingsConnectionString"];
        sqlConnBuilder.PersistSecurityInfo = true;
    });
})
```

Using EF DbOptions example:

```c#
...
.UseOptionsFromSQLEF(settings =>
{
    ...
    settings.WithSQLConnection(sqlConnBuilder =>
    {
        sqlConnBuilder.ConnectionString = builder.Configuration["ConnectionStrings:SettingsConnectionString"];
    }, dbOptions =>
    {
        dbOptions.EnableRetryOnFailure(3, new(0, 0, 5), null);
    });
})
```

### SQLEFSourceSettings
SQLEFSourceSettings is a class that configures the repository:
- ConnectionString - Your typical SQL connection string.
- TableName - (string) What table to use in SQL (Default "Settings")
- KeyColumnName - (string) The Key column name to use (Default "Key")
- ValueColumnName - (string) The Value column name to use (Default "Value")
- EncryptSettings - (bool) Whether or not to encrypt the value using Salted AES (Default false)
- EncryptionKey - (string) The key passed to AES, minimum length is 12 charicters;

SQLEFSourceSettings has a handy `WithSQLConnection(Action<SqlConnectionStringBuilder> builder, Action\<SqlServerDbContextOptionsBuilder>? sqlServerDbContextOptionsBuilder)` method

SQLSourceSettings has a handy `WithSQLConnection(Action<SqlConnectionStringBuilder> builder)` method

These allow you to build the connection string and the DbContext options fluently, like you would any normal SQL Connection.

System.Data.SqlClient.[SqlConnectionStringBuilder](https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnectionstringbuilder)

Microsoft.EntityFrameworkCore.Infrastructure.[SqlServerDbContextOptionsBuilder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.infrastructure.sqlserverdbcontextoptionsbuilde)

AES is provided by [System.Security.Cryptography](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes)
```c#
    BlockBitSize = 128;
    KeyBitSize = 256;
    SaltBitSize = 64;
    Iterations = 10000;
```

## Occasus.BlazorUI

[![Nuget](https://img.shields.io/nuget/v/Occasus.BlazorUI)](https://www.nuget.org/packages/Occasus.BlazorUI/)

[![GitHub last commit](https://img.shields.io/github/last-commit/OFark/Occasus)](https://github.com/OFark/Occasus)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/OFark/Occasus/nuget.yml?branch=main)

```PowerShell
dotnet add package Occasus.BlazorUI
```

```PowerShell
Install-Package Occasus.BlazorUI
```

The Blazor UI package adds a UI component to the configurable path /occasus

Simple usage:
```c#
var builder = WebApplication.CreateBuilder(args);

builder.AddOccasusUI()
```

```c#
var app = builder.Build();
app.UseOccasusUI("mypassword");
```

Or if you need to move Occasus to another URL you can use:
```c#
var app = builder.Build();
app.UseOccasusUI("mypassword", "/other");
```

### Attributes

You can decorate your Options Class with several attributes to determing how the control is shown in the UI.

```c#
public record UserDetails
{
    [Display(Name = "A User Name"), Required]
    public string? User { get; set; }
    [Input(InputType.Password), RestartRequired]
    public string? Password { get; set; }
}
```

The 'RestartRequired' attribute is used to tell the UI to display a message whenever the class or the property (depending on where you put the Attribute) is changed, that the application needs restarting for the option to have an effect. Note: Whether or not the option has an effect is down to your application, Occasus will change it real time regardless.

### Occasus.FeatureManagement

[![Nuget](https://img.shields.io/nuget/v/Occasus.FeatureManagement)](https://www.nuget.org/packages/Occasus.FeatureManagement/)

[![GitHub last commit](https://img.shields.io/github/last-commit/OFark/Occasus)](https://github.com/OFark/Occasus)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/OFark/Occasus/nuget.yml?branch=main)

```PowerShell
dotnet add package Occasus.FeatureManagement
```

```PowerShell
Install-Package Occasus.FeatureManagement
```

FeatureManagement integrates with [Microsoft.FeatureManagement.AspNetCore](https://learn.microsoft.com/en-us/azure/azure-app-configuration/use-feature-flags-dotnet-core?tabs=core5x)

Usage:
```c#
.WithFeatureFlagOptions<FeatureManagement>(builder.Configuration);
```

In order to add feature flags the method requres the IConfiguration interface. This makes it hard to use with Console Apps and Azure Functions. But the inteded use of Feature Flags is API and UI apps.

`WithFeatureFlagOptions` also supports the OptionsBuilder parameter.

Example Feature Flag (This is using the standard .Net feature)

```c#
public FeatureManagedService(IFeatureManager featureManager)
{
    ...
}

public string GetValue(string flag)
{
    ...
    return featureManager.IsEnabledAsync(flag) ? "Yes" : "No";
}
```

As you can see feature flags are only ever booleans, and as such a feature flag class added to Occasus should just be booleans. However, the FeatureManagement system from Microsoft does support extended information to vary the feature flag by various different inputs. This, in theory should still work with Occasus if you structure your class correctly. It is untested at this time.