using Microsoft.EntityFrameworkCore;
using TGRMXInventario.Models;

var builder = WebApplication.CreateBuilder(args);

// ================= 1. REGISTRO DE SERVICIOS (Antes de builder.Build) =================
builder.Services.AddControllersWithViews();

// Habilitar el caché y el servicio de sesiones en el contenedor de dependencias
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    // CAMBIADO: Se incrementó el tiempo de inactividad de 1 a 30 minutos
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// Tus registros de DbContext (AppDbContext, UserContext) van aquí...
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<UserContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UserConnection")));

var app = builder.Build();

// ================= 2. PIPELINE DE MIDDLEWARES (Después de builder.Build) =================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting(); // <--- 1° RUTAS (DEBE IR PRIMERO)

// === EL TRUCO DE LA SOLUCIÓN ESTÁ AQUÍ ===
// Debe ir estrictamente DESPUÉS de UseRouting y ANTES de UseAuthorization
app.UseSession(); // <--- 2° SESIÓN (SE AGREGA AQUÍ)

app.UseAuthorization(); // <--- 3° AUTORIZACIÓN

// Mapeo final de controladores
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Descontar}/{action=Index}/{id?}");

app.Run();
