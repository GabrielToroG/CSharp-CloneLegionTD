# Arquitectura objetivo del proyecto (Godot + C#)

Este documento define la **arquitectura destino** del proyecto para que un proceso de vibecoding o refactorización pueda **mover el código existente** hacia una estructura limpia, escalable y mantenible.

## Objetivo

Reorganizar **todo el código actual** hacia una arquitectura con estas propiedades:

- separación clara por responsabilidades
- dominio desacoplado de Godot
- contenido data-driven
- lectura de estado controlada
- casos de uso explícitos
- crecimiento futuro hacia modos más complejos y multiplayer
- evitar `GameManager` / `BattleManager` gigantes

---

# Principios obligatorios

## 1. Domain no depende de Godot
Nada dentro de `Domain/`, `Application/`, `Queries/`, `GameData/` o `Foundation/` debe depender de:
- `Node`
- `Node2D`
- `Control`
- `SceneTree`
- componentes visuales de Godot

Godot debe vivir solamente en `Platform/Godot/`.

## 2. UI no habla directo con Domain
El flujo correcto es:

`UI / Input -> Application -> Domain`

No hacer:

`UI -> Domain`

## 3. Queries solo leen
La capa `Queries/` existe para lectura controlada del estado.  
No debe mutar datos ni ejecutar reglas de negocio.

## 4. Cada estado tiene un dueño claro
Ejemplos:
- oro e income -> `Domain/Economy`
- fase actual -> `Domain/Match`
- ocupación espacial -> `Domain/Board`
- wave actual -> `Domain/Waves`
- estadísticas acumuladas -> `Domain/Statistics`

## 5. GameData define contenido, Domain define reglas
- `GameData/` describe builders, units, waves, enemies, sends, etc.
- `Domain/` define cómo se comporta todo eso en runtime

## 6. Platform adapta, no decide
`Platform/` no debe contener reglas importantes del juego.
Su responsabilidad es:
- renderizar
- sincronizar con nodos
- recibir input
- persistir datos
- debuggear

---

# Estructura de carpetas objetivo

```text
src/
├── Foundation/
│   ├── Time/
│   ├── Random/
│   ├── Logging/
│   ├── Result/
│   ├── Ids/
│   └── Eventing/
│
├── Platform/
│   ├── Persistence/
│   └── Godot/
│       ├── Presentation/
│       ├── Simulation/
│       ├── Input/
│       └── Debug/
│
├── GameData/
│   ├── Loader/
│   ├── Validator/
│   ├── Registry/
│   ├── Definitions/
│   └── Dtos/
│
├── Domain/
│   ├── Match/
│   ├── Economy/
│   ├── Board/
│   ├── Construction/
│   ├── Roster/
│   ├── Combat/
│   ├── Units/
│   ├── Enemies/
│   ├── Waves/
│   ├── Leaks/
│   ├── Base/
│   ├── Sends/
│   ├── Modes/
│   └── Statistics/
│
├── Application/
│   ├── Match/
│   ├── Construction/
│   ├── Economy/
│   ├── Waves/
│   └── Combat/
│
├── Queries/
│   ├── Board/
│   ├── Units/
│   ├── Enemies/
│   ├── Waves/
│   ├── Economy/
│   ├── Combat/
│   └── Statistics/
│
└── CompositionRoot/
    ├── Container.cs
    ├── ServiceRegistration.cs
    ├── GameModule.cs
    └── Bootstrapper.cs
```

---

# Explicación de los módulos

## `Foundation`
Base técnica transversal del proyecto.

Aquí van utilidades genéricas y reutilizables que no pertenecen al juego como negocio:
- tiempo
- random
- logging
- ids
- resultados/errores
- eventos

Debe ser estable y cambiar poco.

## `Platform`
Implementación concreta del entorno.

Aquí vive todo lo dependiente de:
- Godot
- escenas
- nodos
- input real
- persistencia
- herramientas de debug

Es la capa de adaptación al engine y a la plataforma.

## `GameData`
Contenido estático del juego.

Aquí se definen:
- builders
- units
- enemies
- waves
- sends
- auras
- efectos
- stats base

Debe permitir agregar contenido sin tocar reglas del dominio.

## `Domain`
Núcleo del juego.

Aquí viven las reglas reales:
- fases del match
- economía
- tablero
- construcción
- combate
- units y enemies runtime
- waves
- leaks
- base
- sends
- modos
- estadísticas

No debe depender de Godot.

## `Application`
Casos de uso.

Coordina acciones completas del sistema:
- iniciar partida
- construir unidad
- iniciar wave
- comprar send
- resolver combate

Es el punto de entrada controlado entre exterior y dominio.

## `Queries`
Lectura organizada del estado.

Sirve para responder preguntas como:
- cuántos enemigos quedan
- cuánto oro hay
- cuántas unidades siguen vivas
- cuántos leaks acumula la partida

Solo lectura.

## `CompositionRoot`
Punto de ensamblaje.

Aquí se:
- registran dependencias
- configuran implementaciones
- construyen servicios
- arranca el sistema

No contiene reglas de gameplay.

---

# Explicación de submódulos

## Foundation

### `Foundation/Time`
Herramientas de tiempo de simulación:
- reloj de simulación
- countdowns
- intervalos
- timers reutilizables

No define reglas de negocio; solo entrega tiempo.

### `Foundation/Random`
Generación controlada de aleatoriedad.  
Sirve para centralizar random y volverlo reemplazable o testeable.

### `Foundation/Logging`
Registro de logs, warnings y errores.  
Útil para debugging y observabilidad.

### `Foundation/Result`
Representación estándar de éxito/error.  
Muy útil para validaciones y operaciones de dominio.

### `Foundation/Ids`
Ids tipados del sistema.  
Evita strings o ints mágicos en todo el proyecto.

### `Foundation/Eventing`
Sistema de eventos desacoplado.  
Permite notificar sucesos como:
- enemigo muerto
- wave completada
- oro cambiado
- unidad construida

## Platform

### `Platform/Persistence`
Guardado y carga de información persistente:
- settings
- config
- progreso
- datos de sesión si aplica

### `Platform/Godot/Presentation`
UI y pantallas.  
Ejemplos:
- HUD
- panel de construcción
- panel de sends
- selección de builder
- menús visuales

No debe tener reglas de negocio importantes.

### `Platform/Godot/Simulation`
Adaptación entre dominio y nodos/escenas.  
Ejemplos:
- factories de nodos
- sincronización dominio -> escena
- mapeo de entidades a nodos

### `Platform/Godot/Input`
Recepción y enrutamiento de input.  
Ejemplos:
- clicks para construir
- selección
- cámara
- shortcuts

Debe enviar acciones hacia `Application`.

### `Platform/Godot/Debug`
Herramientas de desarrollo.  
Ejemplos:
- agregar oro
- spawnear enemigos
- saltar waves
- paneles de debug

## GameData

### `GameData/Loader`
Carga datos desde JSON, resources u otras fuentes.

### `GameData/Validator`
Valida integridad y consistencia:
- ids válidos
- referencias existentes
- stats coherentes
- waves bien formadas

### `GameData/Registry`
Punto central de acceso a definiciones cargadas.

### `GameData/Definitions`
Definiciones estáticas del contenido del juego:
- builder definitions
- unit definitions
- enemy definitions
- wave definitions
- send definitions
- aura definitions
- status effect definitions

### `GameData/Dtos`
Estructuras intermedias para carga/deserialización.  
No mezclar DTOs externos con modelos limpios del dominio.

## Domain

### `Domain/Match`
Controla el flujo global de la partida:
- fase actual
- transición entre fases
- condiciones de avance
- reinicio
- estado general del match

### `Domain/Economy`
Dueño de la economía:
- gold
- income
- rewards
- costos
- gastos

### `Domain/Board`
Dueño del espacio jugable:
- topología
- ocupación
- rutas
- zonas
- queries espaciales

### `Domain/Construction`
Reglas de construcción:
- build
- sell
- upgrade
- validaciones
- costos asociados

### `Domain/Roster`
Catálogo disponible del jugador:
- builder seleccionado
- unidades disponibles
- prerequisitos
- árbol de acceso

### `Domain/Combat`
Núcleo del combate.  
Responsable de:
- daño
- armadura
- targeting
- ejecución de ataques
- buffs y debuffs
- auras
- triggers
- stats runtime

### `Domain/Units`
Entidades runtime del jugador.  
Representa unidades ya existentes en partida.

### `Domain/Enemies`
Entidades runtime enemigas.  
Representa creeps/enemigos activos en partida.

### `Domain/Waves`
Generación y control runtime de waves:
- composición
- spawn
- progreso
- finalización

### `Domain/Leaks`
Manejo de enemigos que atraviesan la defensa y generan consecuencia.

### `Domain/Base`
Base del jugador:
- hp
- daño recibido
- estado alive/dead

### `Domain/Sends`
Sistema de sends:
- compra
- cola
- resolución según modo o política

### `Domain/Modes`
Variaciones de reglas por modo de juego:
- single player
- coop
- versus
- endless
- otros futuros

### `Domain/Statistics`
Datos acumulados e históricos:
- leaks totales
- kills totales
- oro ganado
- métricas agregadas

## Application

### `Application/Match`
Casos de uso del flujo global:
- iniciar partida
- avanzar fase
- reiniciar

### `Application/Construction`
Casos de uso de construcción:
- build
- sell
- upgrade

### `Application/Economy`
Casos de uso económicos:
- comprar send
- aplicar income
- ejecutar recompensas

### `Application/Waves`
Casos de uso de waves:
- iniciar wave
- completar wave
- avanzar estado relacionado

### `Application/Combat`
Casos de uso del combate:
- resolver ataque
- avanzar efectos
- procesar pasos del combate

## Queries

### `Queries/Board`
Lecturas del tablero:
- ocupación
- entidades en rango
- zonas
- posiciones válidas

### `Queries/Units`
Lecturas de unidades del jugador:
- cuántas hay vivas
- cuáles están activas
- filtros por tipo o dueño

### `Queries/Enemies`
Lecturas de enemigos:
- vivos
- pendientes
- en ciertas zonas

### `Queries/Waves`
Lecturas del estado de la wave:
- enemigos vivos
- enemigos pendientes de spawn
- wave actual
- si ya terminó

### `Queries/Economy`
Lecturas de economía:
- gold
- income
- datos que UI necesite mostrar

### `Queries/Combat`
Lecturas de estado de combate:
- buffs activos
- auras activas
- estados de combate de entidades

### `Queries/Statistics`
Lecturas históricas/acumuladas:
- leaks totales
- kills totales
- oro total ganado
- métricas de resumen

## CompositionRoot

### `CompositionRoot/Container.cs`
Contenedor de dependencias.  
Registra y resuelve servicios.

### `CompositionRoot/ServiceRegistration.cs`
Registro centralizado de servicios e implementaciones.

### `CompositionRoot/GameModule.cs`
Agrupación modular de registros/configuración del juego.

### `CompositionRoot/Bootstrapper.cs`
Punto de arranque del sistema.

---

# Reglas de migración del código existente

## Regla 1
Si un archivo usa `Node`, `Node2D`, `Control` o escenas de Godot:
- debe vivir en `Platform/Godot/...`
- o dividirse para dejar la lógica en `Domain/` y el adaptador en `Platform/Godot/`

## Regla 2
Si un archivo contiene reglas reales del juego:
- debe ir a `Domain/`

Ejemplos:
- cálculo de daño
- validación de construcción
- lógica de rewards
- progreso de wave
- detección de leaks

## Regla 3
Si un archivo es un punto de entrada de acciones:
- debe ir a `Application/`

Ejemplos:
- construir unidad
- iniciar wave
- comprar send

## Regla 4
Si un archivo solo consulta estado:
- debe ir a `Queries/`

## Regla 5
Si un archivo describe contenido estático:
- debe ir a `GameData/`

## Regla 6
Si un archivo es técnico y genérico:
- debe ir a `Foundation/`

## Regla 7
Si un archivo ensambla dependencias:
- debe ir a `CompositionRoot/`

---

# Ownership del estado

Cada dato importante debe tener un dueño único.

## Dueños recomendados

- `Gold` -> `Domain/Economy`
- `Income` -> `Domain/Economy`
- `CurrentPhase` -> `Domain/Match`
- `BoardOccupancy` -> `Domain/Board`
- `SelectedBuilder` -> `Domain/Roster`
- `ActiveWave` -> `Domain/Waves`
- `ActiveEffects` -> `Domain/Combat`
- `BaseHp` -> `Domain/Base`
- `HistoricalMetrics` -> `Domain/Statistics`

---

# Qué no hacer durante la migración

- no crear un nuevo `GameManager` gigante
- no meter lógica de dominio en `Presentation`
- no dejar queries mutando estado
- no mezclar definiciones estáticas con entidades runtime
- no permitir que muchos módulos muten el mismo dato
- no usar Godot types dentro de `Domain/`
- no hablar desde UI directo al dominio

---

# Convenciones de nombres recomendadas

## Definitions
Contenido estático:
- `UnitDefinition`
- `WaveDefinition`
- `EnemyDefinition`

## State
Estado mutable:
- `EconomyState`
- `MatchState`
- `BaseState`

## Entity
Entidad runtime:
- `UnitEntity`
- `EnemyEntity`

## Service
Reglas y comportamiento de dominio:
- `EconomyService`
- `ConstructionService`
- `WaveService`

## UseCase
Punto de entrada de application:
- `BuildUnitUseCase`
- `StartWaveUseCase`

## QueryService
Lectura:
- `WaveQueryService`
- `EconomyQueryService`

## Adapter / Mapper / Factory
Integración con platform/Godot:
- `BoardMapper`
- `EnemyNodeFactory`
- `DomainToNodeSyncService`

---

# Resultado esperado de la migración

Al final de la migración, el proyecto debe quedar así:

- Godot solo en `Platform/`
- reglas del juego en `Domain/`
- datos estáticos en `GameData/`
- acciones coordinadas en `Application/`
- lecturas en `Queries/`
- herramientas base en `Foundation/`
- ensamblaje en `CompositionRoot/`

Eso permitirá:
- escalar mejor
- mantener el proyecto
- agregar contenido más rápido
- testear reglas sin depender del engine
- crecer a modos más complejos sin rehacer todo

---

# Instrucción final para el proceso de vibecoding

Tomar el código actual y refactorizarlo progresivamente para que:

1. cada archivo sea movido al módulo correcto
2. la lógica de dominio salga de Godot si está mezclada
3. las dependencias se inviertan cuando sea necesario
4. el flujo exterior siempre pase por `Application`
5. las lecturas complejas usen `Queries`
6. el estado tenga ownership claro
7. el proyecto completo respete esta estructura como arquitectura oficial
