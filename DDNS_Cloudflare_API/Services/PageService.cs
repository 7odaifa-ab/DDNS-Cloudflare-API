using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;

namespace DDNS_Cloudflare_API.Services
{
    /// <summary>
    /// Service that provides pages for navigation.
    /// </summary>
    public class PageService : INavigationViewPageProvider
    {
        /// <summary>
        /// Service which provides the instances of pages.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates new instance and attaches the <see cref="IServiceProvider"/>.
        /// </summary>
        public PageService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets a page instance of the specified type.
        /// </summary>
        public T? GetPage<T>()
            where T : class
        {
            if (!typeof(FrameworkElement).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException("The page should be a WPF control.");

            return (T?)_serviceProvider.GetService(typeof(T));
        }

        /// <summary>
        /// Gets a page instance of the specified type.
        /// </summary>
        public object? GetPage(Type pageType)
        {
            if (!typeof(FrameworkElement).IsAssignableFrom(pageType))
                throw new InvalidOperationException("The page should be a WPF control.");

            return _serviceProvider.GetService(pageType);
        }
    }
}
