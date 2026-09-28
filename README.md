# Luar Café VR — Google Cardboard (Android)

App de realidad virtual con **Gaze**, **teletransportación** y **agarre/posicionamiento de objetos**, ambientada en una cafetería *cute, cozy* y pastel. Las 4 escenas comparten exactamente la misma identidad visual: piso ajedrezado crema/rosa, zócalo crema, paredes rosa empolvado, ventanas en arco, toldo a rayas, letrero neón "Luar Café", lámparas colgantes, plantas y paleta pastel desaturada (mate, sin brillos) para evitar fatiga visual.

- Unity **6000.6.0f1** · URP (perfil móvil, sin HDR, MSAA 2x) · **Google Cardboard XR Plugin 1.35**
- APK: `Builds/LuarCafeVR.apk` (Android 8.0+ / API 26, ARM64, OpenGL ES 3)

## Escenas (`Assets/Scenes`)

| # | Escena | Qué hay |
|---|--------|---------|
| 0 | **Inicio** | Cartel de bienvenida, paneles "¿Cómo se juega?" y "Escenas", botones **Tutorial / Recorrido / Salir**. |
| 1 | **Tutorial** | 4 pasos guiados con flechas: mirar un botón → teletransportarse → agarrar la taza → colocarla en el plato. Al final: *Ir al Recorrido* / *Menú*. |
| 2 | **Recorrido** | Cafetería completa. 3 clientes con globos de pedido (café, cupcake, macarons). La cafetera prepara el café al mirarla. 5 puntos de salto. Panel con progreso, mensajes y botón *Menú*. Pedido equivocado → aviso y sonido. |
| 3 | **Felicitaciones** | Confeti, globos, banderines, estrella, torta con velas, resumen (pedidos, tiempo, errores) y botones *Jugar otra vez / Menú principal / Salir*. |

## Controles

- **Puntero Gaze**: punto blanco con borde cacao en el centro de la vista (contraste sobre fondos claros y oscuros). Al mirar algo interactivo aparece un anillo que se llena (1,2–2 s) y una etiqueta con la acción ("Recoger café", "Servir aquí", "Mesa 2"…).
- **Activar**: mantener la mirada hasta llenar el anillo **o** tocar el botón del visor Cardboard.
- **Moverse**: mirar un círculo menta del piso (o su flecha flotante). Transición con fundido suave para no marear.
- **Agarrar / colocar**: mirar un objeto → flota frente a ti y se mueve contigo → mirar un plato con aro rosado (o cualquier mesa/barra) para dejarlo.
- **Salir**: botón *Salir* o la **X** del visor. El engranaje permite escanear el QR del visor.
- **En el editor (sin visor)**: clic derecho (o Alt) + mover ratón para mirar; clic izquierdo o espacio = botón del visor.

## Cómo regenerar / compilar

Menú **Luar Café VR** en Unity:
1. *Configurar proyecto (Android + Cardboard)* — URP, Player Settings, XR loader.
2. *Construir las 4 escenas* — genera todo desde código (`Assets/Luar/Editor/SceneBuilder.cs` + `CafeKit.cs`).
3. *Compilar APK* → `Builds/LuarCafeVR.apk`.
- *Prueba automática (Play Mode)* — simula la mirada y recorre Tutorial → Recorrido → Felicitaciones → Inicio.
- *Capturas de las escenas* — renderiza vistas a `Captures/`.

Por línea de comandos:
```
Unity.exe -batchmode -quit -projectPath . -executeMethod Luar.EditorTools.ProjectSetup.BuildApk
```

## Instalar en el celular
1. Activar *Opciones de desarrollador* → *Depuración USB* (o permitir instalar APK de origen desconocido).
2. `adb install -r Builds/LuarCafeVR.apk` (o copiar el APK al teléfono y abrirlo).
3. Abrir la app, escanear el QR del visor (o *Omitir*), poner el teléfono en el Cardboard.

## Código (`Assets/Luar/Scripts`)
- `Gaze/` — `GazePointer` (raycast, dwell, retícula, etiqueta), `GazeInteractable` (base con resalte suave), `GazeButton`.
- `Movement/` — `PlayerRig`, `TeleportPoint`, `LazyFollowPanel` (panel de instrucciones que se reubica tras cada salto).
- `Interaction/` — `Grabbable`, `PlacementZone`, `PlaceableSurface`, `ItemDispenser` (cafetera).
- `Game/` — `TutorialController`, `OrderManager`, `CelebrationController`, `GameStats`.
- `Core/` — `SceneFlow` (cambios de escena con fundido), `ScreenFader`, `CardboardSession`, `VRInput`, `Sfx` (sonidos generados por código).
- `Animation/` — `Spin`, `Bob`, `Sway`, `Pulse`, `LightFlicker`, `Billboard`.
