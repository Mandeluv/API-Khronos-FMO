# API Khronos FMO — ampliación parcial

Base: ZIP master, commit e1f79e633327b9536baced4b856cf698526c10a0. Se conservan controladores, DTOs y servicios originales. No se modificaron las dependencias ni el esquema PostgreSQL.

## Ejecutar
1. Abrir API-Khronos-FMO.slnx en Visual Studio 2026; restaurar NuGet y compilar con .NET 10.
2. Iniciar PostgreSQL 17 en Docker. En User Secrets configurar ConnectionStrings:Postgres con Host=localhost;Port=5434;Database=Khronos-FMO;Username=postgres;Password=<contraseña local>. No subir secretos al repositorio.
3. Ejecutar perfil https, abrir /swagger. Ejecutar SQL/verificar_objetos.sql en pgAdmin para verificar nombres de vistas y firmas de procedimientos.

## Nuevos endpoints (GET)
- /api/Grupos
- /api/Grupos/{id}/estudiantes
- /api/Reservas/aprobadas, /rechazadas, /canceladas
- /api/Incidencias
- /api/Estadisticas/uso-por-solicitante
- /api/Historial/reservas
- /api/Catalogos/ubicaciones, /asignaturas, /recursos
- /api/Disponibilidad?fecha=2027-01-12&horaInicio=08:00&horaFin=10:00&capacidad=20

Los catálogos derivan de vistas existentes y solo contienen elementos asociados a registros; no son catálogos maestros completos. Las nuevas consultas devuelven JSON con las columnas originales de PostgreSQL (snake_case); las anteriores conservan sus DTOs. Disponibilidad excluye pendientes y aprobadas, bloqueos y clases ordinarias; es informativa y no garantiza una reserva. La fecha de referencia usa America/El_Salvador. Los nombres de vistas adicionales se tomaron de la documentación previa y requieren verificar contra el SQL real.

## Transacciones
El proyecto original tiene cinco operaciones explícitas. Se añade una sexta al registro de horarios ordinarios; se cierra el lector antes de confirmar y se revierte ante excepciones. Los procedimientos deben emitir excepciones para errores o definir un contrato inequívoco de resultados: si devuelven mensajes de error sin lanzar excepción, todavía se debe implementar el tratamiento de esos resultados antes de confirmar. No se ha verificado ese contrato sin el script actualizado. Probar en una BD de pruebas: ejecutar una operación válida, una que falle, verificar tablas e historial antes y después. Probar dos reservas simultáneas; la consulta de disponibilidad no sustituye restricciones o bloqueos de base de datos.

## Pendiente
- Autenticación y autorización, hash de contraseñas, permisos, identidad del usuario autenticado. Los endpoints actuales no están protegidos; usar solo en desarrollo local, no exponer públicamente.
- Escritura de usuarios, roles, perfiles, incidencias, recursos, categorías, ubicaciones, asignaturas, grupos y estudiantes; desbloqueo, asistencia, modificación de horarios y CRUD administrativo. Requieren DDL y reglas reales. No se inventaron columnas ni permisos.
- Verificar resultados OUT de procedimientos, concurrencia, cobertura de historial, pruebas de integración. No se compiló en este entorno (SDK dotnet no instalado), ni se ejecutó contra PostgreSQL.

## Pruebas manuales
GET /api/PruebaConexion: 200. Probar cada nueva ruta: 200 con lista (vacía si no hay datos), comprobar contra su vista SQL. Grupos/0/estudiantes: 400. Disponibilidad sin fecha o con horario invertido: 400. Probar fecha futura laborable y comprobar que no retorna espacios con reservas, bloqueos o clases que se crucen. Para horarios, enviar datos válidos y después repetir un horario incompatible; verificar que no se creó ningún registro adicional cuando PostgreSQL emite excepción.

## Segunda ampliación (base2, PostgreSQL 17)

- Ejecutar `SQL/01_integridad.sql` en una copia de la BD desde pgAdmin antes de probar. No restaurar el dump sobre una base activa. El dump original no se incluye por contener datos personales y credenciales.
- La API exige autenticación. Se utiliza el bearer token opaco integrado de ASP.NET Core (no JWT). Los roles se toman de tbl_rol; los DTO originales mantienen CarnetUsuario y se valida que coincida con la identidad. Las listas generales sensibles se limitan al administrador; faltan filtros por espacio en varias consultas originales.
- Restablecer claves antiguas localmente desde la carpeta que contiene el csproj. PowerShell: `$env:KHRONOS_CARNET = 'A001'`; `$env:KHRONOS_CLAVE = Read-Host 'Nueva clave (12 caracteres o más)'`; `dotnet run -- --restablecer-clave`; `Remove-Item Env:KHRONOS_CLAVE`. Usar entorno Development y cadena configurada en User Secrets; repetir con cuentas de prueba. No se aceptan claves antiguas en texto plano. El script SQL no cambia credenciales.
- POST /api/Auth/login con JSON `{ "carnet": "A001", "clave": "..." }`, copiar accessToken y enviar `Authorization: Bearer <token>` en el cliente HTTP (Swagger si permite introducir header, o Postman/curl). GET /api/Auth/yo verifica identidad. Token expira según configuración por defecto; no hay refresh implementado.
- Nuevas escrituras: Usuarios, Espacios, Academico, Catalogos, Incidencias, desbloqueo, eliminación de bloqueo y horario, asistencia. El servicio EscrituraService controla BEGIN/COMMIT/ROLLBACK y comparte un bloqueo transaccional con los servicios anteriores. El bloqueo solo coordina clientes que lo adquieren; escrituras directas no quedan protegidas de concurrencia.
- Validar en copia: crear usuario con correo duplicado (no debe quedar perfil huérfano); enviar carnet de otro usuario (403); aprobar reserva (historial); cancelar tras inicio (rechazo); dos solicitudes simultáneas solapadas (solo una); registrar asistencia después de fin; resolver incidencia del propio espacio.
- Pendiente: pruebas de compilación e integración, CRUD completo, carga masiva Excel, edición de horarios, filtrado por encargado de todos los listados, JWT si es requisito, restricciones de capacidad al inscribir y protección de concurrencia para escrituras SQL externas. No desplegar sin revisar permisos y credenciales.
