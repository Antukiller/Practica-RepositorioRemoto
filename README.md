# 6. PRACTICA REPOSITORIO-REMOTO

## INTEGRANTES
-Diego Gonzalez Manzanero / Antoine Lopez Amir

## 1. INTRODUCCION Y OBJETIVOS

### 1.1 DESCRIPCION GENERAL
Crear un servicio web REST desarrollado con .NET orientado a una gestion eficiente de identidades de usuario mediante una estrategia de almacenamien en tres niveles dividiendose en una Cache en memoria o Redis, Base de datos SqLite o PostGress y una API REST externa. Incorpora tambien procesamientos de eventos asincronos desacoplados mediente programacion reacctiva y sincrinizacion periodica.

### 1.2 OBJETIVOS TECNICOS

- Se implementa una arquitectura por capas aplicando el patron Result
- Gestionar la jerarquia de persistencia 
- Automatizar la consistencia de datos mediante un **BackgroundService** ciclico y tareas de iinicializacion en el arranque
- Se implementa un bus de eventos en las notificaciones con **System.Reactive**
- Garantar la calidad de software mediente pruebas unitarias exhaustivas con NUnit, Moq y FluentAssertions

## 2. ANALISIS

### 2.1 REQUISITOS FUNCIONALES (RF)
- RF-01 (Inicialización del sistema): Al arrancar el servicio, se debe borrar la base de datos local SQLite y repoblarse íntegramente desde la API REST remota.
- RF-02 (Sincronización cíclica): Cada 60 segundos, un servicio en segundo plano debe vaciar la caché en memoria, purgar la base de datos local y recargarla con los datos frescos de la API externa.
- RF-03 (Consultar todos los usuarios): GET /api/users debe retornar la lista de usuarios desde la BD local. Si la BD estuviese vacía, debe recurrir a la API remota, persistir y retornar.
- RF-04 (Consultar usuario por ID): GET /api/users/{id} debe buscar secuencialmente en Caché $\rightarrow$ BD Local $\rightarrow$ API REST. Cada acierto en niveles inferiores debe poblar los superiores. Retorna 404 Not Found si no existe.
- RF-05 (Creación de usuario): POST /api/users valida la petición, la envía a la API remota, almacena el resultado en BD local, actualiza la caché y emite un evento reactivo de creación. Código de respuesta: 201 Created.
- RF-06 (Actualización de usuario): PUT /api/users/{id} propaga la actualización a la API remota, actualiza BD local y caché, y emite notificación reactiva. Retorna 200 OK o 404 Not Found.
- RF-07 (Eliminación de usuario): DELETE /api/users/{id} solicita la baja remota, borra el registro de SQLite y de la caché, y emite notificación reactiva. Retorna 204 No Content o 404 Not Found.
- RF-08 (Exportación de datos): GET /api/users/export genera un archivo .json con el estado actual de los usuarios en el sistema de ficheros del servidor y devuelve la ruta absoluta o relativa generada.RF-09 (Notificaciones reactivas): Cada operación de creación, actualización o eliminación debe notificar a un bus reactivo expuesto a través de IObservable<T> cuyos suscriptores muestren trazas formateadas en la consola estándar.

### 2.2 REQUISITOS NO FUNCIONALES (RNF)
- RNF-01 (Asincronía y Concurrencia): Todas las operaciones de entrada/salida (I/O) a disco, base de datos y red deben ser asíncronas no bloqueantes utilizando Task<T>, ValueTask<T> y CancellationToken.
- RNF-02 (Rendimiento en Lectura): Las lecturas repetidas de un mismo usuario deben resolverse en memoria ($O(1)$) mediante MemoryCache, evitando accesos innecesarios a base de datos y red.
- RNF-03 (Manejo Robusto de Errores): Cero excepciones no controladas utilizadas para lógica de control de flujo. La capa de negocio debe devolver tipos monádicos Result<T, DomainError>.
- RNF-04 (Mantenibilidad y Código Limpio): Uso de Primary Constructors de C# 14, separación de responsabilidades estricta (Separation of Concerns), contratos desacoplados mediante interfaces e inyección de dependencias nativa.
- RNF-05 (Trazabilidad y Logging): Registro estructurado de eventos de sincronización, errores y operaciones críticas con Serilog en consola y archivo rotativo.
- RNF-06 (Portabilidad): Capacidad de empaquetar y ejecutar la aplicación dentro de contenedores Docker mediante imágenes Linux ligeras (alpine / chiseled).
