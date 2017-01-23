using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

using System.Data.Entity;
using WingtipToys.Models;
using WingtipToys.Logic;

namespace WingtipToys {
    public class Global : HttpApplication {
        void Application_Start(object sender, EventArgs e) {

            // Código que se ejecuta al iniciar la aplicación
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            /* 
             * Inicialización de la base de datos
             * Usamos la confiuguración inicial de la base de datos de los productos
             */
            Database.SetInitializer(new ProductDatabaseInitializer());

            // Creación de roles y usuarios
            RoleActions roleAction = new RoleActions();
            roleAction.AddUserAndRole();

            // Añadimos la tabla de rutas
            RegisterCustomRoutes(RouteTable.Routes);

        }

        void RegisterCustomRoutes(RouteCollection routes) {

            routes.MapPageRoute(
                "ProductsByCategoryRoute",
                "Category/{categoryName}",
                "~/ProductList.aspx");

            routes.MapPageRoute(
                "ProductByNameRoute",
                "Product/{productName}",
                "~/ProductDetails.aspx");

        }
    }
}