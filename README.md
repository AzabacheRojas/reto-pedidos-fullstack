# Gestión de Pedidos — Reto Técnico Fullstack (.NET 8 + React)

Aplicación end-to-end con autenticación JWT y gestión (CRUD) de pedidos.

| Capa | Tecnología |
|---|---|
| Backend | .NET 8, ASP.NET Core Web API, EF Core 8, SQL Server, JWT Bearer, FluentValidation, Polly, Serilog, Swagger |
| Frontend | React 19 + TypeScript, Vite, React Router, Tailwind CSS, Axios |
| Base de datos | SQL Server 2022 (en Docker) |

---

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) 20.19+ o 22.12+
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para SQL Server) — o una instancia propia de SQL Server

---

## Levantar el proyecto

Se necesitan **3 pasos en orden**: base de datos → API → frontend. Usa una terminal para la API y otra para el frontend.

### 1. Descomprimir

Extrae el archivo `.rar` (con WinRAR o 7-Zip) en una carpeta y abre una terminal dentro de ella. Debe contener:

```
backend/
frontend/
README.md
```

> El archivo no incluye `node_modules/`, `bin/` ni `obj/`; se generan en los pasos 3 y 4.

### 2. Base de datos (SQL Server en Docker)

Con Docker Desktop en ejecución, la **primera vez** crea el contenedor:

```bash
docker run -d --name pedidos-sql -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Pedidos_Str0ng!Pass" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

Las siguientes veces solo hay que iniciarlo:

```bash
docker start pedidos-sql
```

> **¿Usas SQL Server local o LocalDB?** Cambia `ConnectionStrings:DefaultConnection` en `backend/src/Pedidos.Api/appsettings.json`, por ejemplo:
> `Server=(localdb)\\MSSQLLocalDB;Database=PedidosDb;Trusted_Connection=True;TrustServerCertificate=True`

No hace falta crear la base de datos ni las tablas: **la API aplica las migraciones y carga datos de ejemplo al iniciar.**

### 3. API (terminal 1)

```bash
cd backend
dotnet run --project src/Pedidos.Api
```

Está lista cuando aparece `Now listening on: http://localhost:5080`.

- Swagger: http://localhost:5080/swagger
- Health check: http://localhost:5080/health

> También puedes abrir `backend/RetoPedidos.slnx` en Visual Studio 2022 y ejecutar el proyecto **Pedidos.Api**.

### 4. Frontend (terminal 2)

```bash
cd frontend
cp .env.example .env        # Windows PowerShell: Copy-Item .env.example .env
npm install
npm run dev
```

Abre http://localhost:5173 e inicia sesión.

### Detener

- API y frontend: `Ctrl + C` en cada terminal.
- SQL Server: `docker stop pedidos-sql`

---

## Usuarios de prueba

| Email | Contraseña | Rol | Permisos |
|---|---|---|---|
| `admin@pedidos.com` | `Admin123*` | Admin | Ver, crear, editar y **eliminar** |
| `user@pedidos.com` | `User123*` | User | Ver, crear y editar |

En la pantalla de login, los botones de **"Accesos de demostración"** completan las credenciales.

---

## Configuración

| Archivo | Clave | Valor por defecto |
|---|---|---|
| `backend/src/Pedidos.Api/appsettings.json` | `ConnectionStrings:DefaultConnection` | `Server=localhost,1433;Database=PedidosDb;User Id=sa;...` |
| | `Jwt:SecretKey` | Clave de desarrollo — **cambiar en producción** |
| | `Jwt:ExpirationMinutes` | `60` |
| | `Cors:AllowedOrigins` | `http://localhost:5173` |
| | `Database:MigrateOnStartup` | `true` |
| `frontend/.env` | `VITE_API_URL` | `http://localhost:5080` |

---

## Comandos útiles

| Dónde | Comando | Qué hace |
|---|---|---|
| `backend/` | `dotnet build` | Compila la solución |
| `backend/` | `dotnet test` | Ejecuta los tests unitarios |
| `frontend/` | `npm run lint` | Revisa el código con ESLint |
| `frontend/` | `npm run build` | Compila para producción (`dist/`) |

---

## Endpoints

| Método | Ruta | Auth | Respuestas |
|---|---|---|---|
| POST | `/auth/login` | Anónimo | 200, 400, 401, 429 |
| GET | `/api/pedidos?page&pageSize&search&estado&sortBy&desc` | Bearer | 200 |
| GET | `/api/pedidos/{id}` | Bearer | 200, 404 |
| POST | `/api/pedidos` | Bearer | 201, 400, 409 |
| PUT | `/api/pedidos/{id}` | Bearer | 200, 400, 404, 409 |
| DELETE | `/api/pedidos/{id}` | Bearer + Admin | 204, 403, 404 |

Los errores se devuelven en formato **ProblemDetails (RFC 7807)**. Para probar desde Swagger: ejecuta `POST /auth/login`, copia el token y pégalo en **Authorize**.

---

## Arquitectura

Clean Architecture: las dependencias apuntan hacia el dominio.

```
Pedidos.Api ──────────► Pedidos.Application ──► Pedidos.Domain
     └──► Pedidos.Infrastructure ──┘
```

| Proyecto | Responsabilidad |
|---|---|
| **Domain** | Entidades (`Pedido`, `Usuario`) que protegen sus reglas de negocio |
| **Application** | Casos de uso, DTOs, validadores (FluentValidation) e interfaces |
| **Infrastructure** | EF Core + SQL Server, repositorios, Unit of Work, JWT, BCrypt, Polly |
| **Api** | Controladores, autenticación/autorización, manejo global de errores, Swagger |

**Frontend** (`frontend/src`): `api/` (axios + interceptores JWT) · `context/` (sesión y notificaciones) · `components/` · `pages/` · `hooks/` · `types/` · `utils/`.

### Seguridad y resiliencia

- JWT Bearer con validación de emisor, audiencia, firma y expiración (60 min); roles `Admin` / `User`.
- Política global que exige autenticación salvo endpoints `[AllowAnonymous]`.
- Contraseñas con BCrypt; rate limiting en `/auth/login` (5 intentos/min por IP → 429); CORS restringido al frontend.
- Reintentos ante errores transitorios de SQL Server (EF Core) y Polly al aplicar migraciones.
- Frontend: token en cada request y logout automático al expirar o recibir 401.

### Reglas de negocio

| Regla | Implementación |
|---|---|
| Total > 0 | Validador (400) + entidad de dominio + `CHECK` en BD |
| Número de pedido único | Servicio (409) + índice único en BD |
| Solo usuarios autenticados | `[Authorize]` + política global |
| Eliminación lógica | `IsDeleted` + filtro global de EF Core |

---

## Solución de problemas

| Síntoma | Causa probable / solución |
|---|---|
| La API falla al iniciar con error de conexión | SQL Server no está corriendo: `docker start pedidos-sql`. Al arrancar el contenedor, espera unos segundos. |
| `docker: Conflict. The container name "/pedidos-sql" is already in use` | El contenedor ya existe: usa `docker start pedidos-sql` en lugar de `docker run`. |
| `port is already allocated` / puerto 1433, 5080 o 5173 ocupado | Otro proceso usa el puerto; deténlo o cambia el puerto. |
| El login falla con "Network Error" | La API no está levantada o `VITE_API_URL` en `frontend/.env` no apunta a `http://localhost:5080`. |
| Error 429 en el login | Más de 5 intentos en un minuto; espera un minuto. |
