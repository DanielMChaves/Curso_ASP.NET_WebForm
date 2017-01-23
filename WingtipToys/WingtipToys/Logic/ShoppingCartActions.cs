using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WingtipToys.Models;

namespace WingtipToys.Logic
{
    public class ShoppingCartActions : IDisposable
    {
        public string ShoppingCartId { get; set; }

        private ProductContext _db = new ProductContext();

        public const string CartSessionKey = "CartId";

        public void AddToCart(int id) {

            // Obtenemos el ID de la tarjeta con la que se va a realizar la compra.           
            ShoppingCartId = GetCartId();

            // Comprobamos si el elemento pedido esta en el carro de compras
            var cartItem = _db.ShoppingCartItems.SingleOrDefault(
                            c => c.CartId == ShoppingCartId
                            && c.ProductId == id);

            if (cartItem == null) {

                /* Si el elemento no esta en el carrito:
                 *  - Creamos un nuevo elemento para el carrito
                 *  - Lo añadimos en el carrito de la DB
                 */                 
                cartItem = new CartItem {
                    ItemId = Guid.NewGuid().ToString(),
                    ProductId = id,
                    CartId = ShoppingCartId,
                    Product = _db.Products.SingleOrDefault(
                                p => p.ProductID == id),
                    Quantity = 1,
                    DateCreated = DateTime.Now
                };

                // Introducimos el elemento en la DB
                _db.ShoppingCartItems.Add(cartItem);
            } else {
                /* Si el elemento esta en el carrito:
                 *  - Incrementamos el contados de dicho producto
                 */                 
                cartItem.Quantity++;
            }

            // Guardamos los cambios realizados en la DB
            _db.SaveChanges();
        }

        public void Dispose() {
            if (_db != null) {
                _db.Dispose();
                _db = null;
            }
        }

        public string GetCartId() {

            // Comprobamos si el usuario tiene un CART ID de sesión
            if (HttpContext.Current.Session[CartSessionKey] == null) {

                // Comprobamos si el usuario esta registrado
                if (!string.IsNullOrWhiteSpace(HttpContext.Current.User.Identity.Name)) {
                    HttpContext.Current.Session[CartSessionKey] = HttpContext.Current.User.Identity.Name;
                } else {
                    /* Como el usuario no esta conectado:
                     *  - Le generamos un GUID (Globally Unique Identifier) usando las clase System.Guid
                     */     
                    Guid tempCartId = Guid.NewGuid();
                    HttpContext.Current.Session[CartSessionKey] = tempCartId.ToString();
                }
            }
            return HttpContext.Current.Session[CartSessionKey].ToString();
        }

        public List<CartItem> GetCartItems() {

            // Obtenemos el CART ID de sesión
            ShoppingCartId = GetCartId();
 
            // Devolvemos la lista de productos que tenga dicho carrito
            return _db.ShoppingCartItems.Where(
                c => c.CartId == ShoppingCartId).ToList();
        }

        public decimal GetTotal() {

            // Obtenemos el CART ID de sesión (ID of the shopping cart for the user)
            ShoppingCartId = GetCartId();

            // Multiply product price by quantity of that product to get        
            // the current price for each of those products in the cart.  
            // Sum all product price totals to get the cart total.
            
            // Declaramos e inicializamos la variable 'total'   
            decimal? total = decimal.Zero;

            // Obtenemos el total
            // La query realiza una llamada recursiva ya que con .sum() pedimos el sumatorio
            total = (decimal?)(from cartItems in _db.ShoppingCartItems
                               where cartItems.CartId == ShoppingCartId
                               select (int?)cartItems.Quantity *
                               cartItems.Product.UnitPrice).Sum();
            return total ?? decimal.Zero;
        }

        public ShoppingCartActions GetCart(HttpContext context)
        {
            using (var cart = new ShoppingCartActions())
            {
                cart.ShoppingCartId = cart.GetCartId();
                return cart;
            }
        }

        public void UpdateShoppingCartDatabase(String cartId, ShoppingCartUpdates[] CartItemUpdates)
        {
            using (var db = new WingtipToys.Models.ProductContext())
            {
                try
                {
                    int CartItemCount = CartItemUpdates.Count();
                    List<CartItem> myCart = GetCartItems();
                    foreach (var cartItem in myCart)
                    {
                        // Iterate through all rows within shopping cart list
                        for (int i = 0; i < CartItemCount; i++)
                        {
                            if (cartItem.Product.ProductID == CartItemUpdates[i].ProductId)
                            {
                                if (CartItemUpdates[i].PurchaseQuantity < 1 || CartItemUpdates[i].RemoveItem == true)
                                {
                                    RemoveItem(cartId, cartItem.ProductId);
                                }
                                else
                                {
                                    UpdateItem(cartId, cartItem.ProductId, CartItemUpdates[i].PurchaseQuantity);
                                }
                            }
                        }
                    }
                }
                catch (Exception exp)
                {
                    throw new Exception("ERROR: Unable to Update Cart Database - " + exp.Message.ToString(), exp);
                }
            }
        }

        public void RemoveItem(string removeCartID, int removeProductID)
        {
            using (var _db = new WingtipToys.Models.ProductContext())
            {
                try
                {
                    var myItem = (from c in _db.ShoppingCartItems where c.CartId == removeCartID && c.Product.ProductID == removeProductID select c).FirstOrDefault();
                    if (myItem != null)
                    {
                        // Remove Item.
                        _db.ShoppingCartItems.Remove(myItem);
                        _db.SaveChanges();
                    }
                }
                catch (Exception exp)
                {
                    throw new Exception("ERROR: Unable to Remove Cart Item - " + exp.Message.ToString(), exp);
                }
            }
        }

        public void UpdateItem(string updateCartID, int updateProductID, int quantity)
        {
            using (var _db = new WingtipToys.Models.ProductContext())
            {
                try
                {
                    var myItem = (from c in _db.ShoppingCartItems where c.CartId == updateCartID && c.Product.ProductID == updateProductID select c).FirstOrDefault();
                    if (myItem != null)
                    {
                        myItem.Quantity = quantity;
                        _db.SaveChanges();
                    }
                }
                catch (Exception exp)
                {
                    throw new Exception("ERROR: Unable to Update Cart Item - " + exp.Message.ToString(), exp);
                }
            }
        }

        public void EmptyCart()
        {
            ShoppingCartId = GetCartId();
            var cartItems = _db.ShoppingCartItems.Where(
                c => c.CartId == ShoppingCartId);
            foreach (var cartItem in cartItems)
            {
                _db.ShoppingCartItems.Remove(cartItem);
            }
            // Save changes.             
            _db.SaveChanges();
        }

        public int GetCount()
        {
            ShoppingCartId = GetCartId();

            // Get the count of each item in the cart and sum them up          
            int? count = (from cartItems in _db.ShoppingCartItems
                          where cartItems.CartId == ShoppingCartId
                          select (int?)cartItems.Quantity).Sum();
            // Return 0 if all entries are null         
            return count ?? 0;
        }

        public struct ShoppingCartUpdates {
            public int ProductId;
            public int PurchaseQuantity;
            public bool RemoveItem;
        }

        public void MigrateCart(string cartId, string userName) {

            // Usamos un cartId existente para buscar el 'shopping cart' de un usuario
            var shoppingCart = _db.ShoppingCartItems.Where(c => c.CartId == cartId);

            // Remplaza el cartId por el nombre de usuario con el que se ha logueado
            foreach (CartItem item in shoppingCart) {
                item.CartId = userName;
            }

            // Actualizamos el la sesión HTTP
            HttpContext.Current.Session[CartSessionKey] = userName;
            // Actualizamos la DB
            _db.SaveChanges();
        }
    }
}