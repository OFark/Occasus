using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace Occasus.FeatureManagement
{
    public static class Extensions
    {
        public static OptionsBuilder<TOption> UseFeatureFlags<TOption>(this OptionsBuilder<TOption> optionBuilder) where TOption : class, new()
        {
            optionBuilder.Configure<IConfiguration>((settings, config) =>
            {
                optionBuilder.Services.AddFeatureManagement(config);
            });
            return optionBuilder;
        }
    }
}