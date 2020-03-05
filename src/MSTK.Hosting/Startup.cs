using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MSTK.Hosting
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        public void ConfigureServices(IServiceCollection services)
        {
            List<Assembly> controllerAssemblies = new List<Assembly>();

            string folder = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetEntryAssembly().CodeBase).Path));

            string[] files = Directory.GetFiles(folder, "*.dll");
            foreach (string file in files)
            {
                Assembly assembly = Assembly.LoadFrom(file);
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(typeof(Controller)))
                    {
                        Hoster.ControllerTypes.Add(type);

                        if (!controllerAssemblies.Contains(assembly))
                            controllerAssemblies.Add(assembly);
                    }
                }
            }

            foreach (Assembly assembly in controllerAssemblies)
                services.AddMvc().AddApplicationPart(assembly).AddControllersAsServices();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app
                .UseCallInfoMiddleware()
                .UseMvc();
        }
    }
}
