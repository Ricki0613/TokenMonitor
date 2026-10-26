<p align="center">
  <img src="assets/TokenMonitor.png" width="72" alt="Token Monitor">
</p>
<h1 align="center">Token Monitor</h1>
<p align="center"><strong>Cuotas, saldos y uso de API de un vistazo.</strong><br>Un monitor de escritorio para Windows compatible con Codex / ChatGPT Work, DeepSeek y OpenAI API.</p>
<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4" alt="Windows 10 / 11 x64">
  <a href="https://github.com/Ricki0613/TokenMonitor/releases/latest"><img src="https://img.shields.io/badge/version-3.2.1-10A37F" alt="Version 3.2.1"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License"></a>
</p>
<p align="center">
  <a href="https://github.com/Ricki0613/TokenMonitor/releases/download/v3.2.1/TokenMonitor-v3.2.1-windows-x64.zip"><strong>Descargar para Windows</strong></a> ·
  <a href="#inicio-rápido">Inicio rápido</a> ·
  <a href="docs/USER_GUIDE.zh-CN.md">Guía de uso (chino)</a>
</p>

<p align="center">
  <a href="README.md">简体中文</a> · <a href="README.en.md">English</a> · <a href="README.ja.md">日本語</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.de.md">Deutsch</a> · <a href="README.ru.md">Русский</a>
</p>

[![Token Monitor](assets/readme-hero.png)](assets/readme-hero.png)

<p align="center"><sub>Panel y esfera flotante en una sola imagen · Datos de ejemplo · Haz clic para ver la imagen en alta resolución</sub></p>

## Tu uso, siempre a mano

Consulta tres servicios en un solo panel mientras programas o utilizas modelos, y mantén los datos que necesitas a la vista en el escritorio.

- **Esfera flotante compacta**: dos porcentajes muestran la cuota restante de 5 horas y la semanal. Arrástrala para moverla, haz doble clic para ver los detalles y elige entre cinco tamaños.
- **Ventanas flotantes independientes**: muestra, oculta, mueve y redimensiona cada servicio por separado. Los servicios conectados siguen actualizándose aunque ocultes sus ventanas.
- **Historial local**: gráficos de 7 y 30 días con conservación de 90 días. Los datos que no se han recogido no se inventan.
- **Aspecto personalizable**: tema claro, oscuro o del sistema, tres tamaños de letra y navegación lateral permanente.
- **Actualización y restauración**: descarga y verifica el paquete desde la aplicación, confirma el reinicio y conserva la versión anterior para poder volver a ella.

## Datos disponibles

| Servicio | Qué puedes consultar | Conexión |
| --- | --- | --- |
| **ChatGPT / Codex** | Cuota restante compartida de Codex / ChatGPT Work, horas de restablecimiento y restablecimientos disponibles | Codex con sesión iniciada en el equipo |
| **DeepSeek** | Saldo disponible, recargado y promocional, separados por moneda | API Key o sesión local de DeepSeek Harness |
| **OpenAI API** | Tokens de hoy y del mes, entrada/salida/entrada en caché y solicitudes para un API Key ID | API Key normal; el historial también requiere una Admin API Key y el Key ID correspondiente |

Los datos de ChatGPT cubren la cuota de Codex / ChatGPT Work, no todos los límites de mensajes del chat. Las estadísticas de OpenAI abarcan el endpoint oficial `usage/completions`. La entrada en caché ya está incluida en los tokens de entrada y no se suma dos veces.

## Inicio rápido

**Requisitos: Windows 10 / 11 x64 y .NET Framework 4.8. La interfaz de la aplicación está actualmente en chino.** No necesitas Python, Node ni el SDK de .NET para ejecutarla.

1. [Descarga el ZIP para Windows](https://github.com/Ricki0613/TokenMonitor/releases/download/v3.2.1/TokenMonitor-v3.2.1-windows-x64.zip) y extrae todo su contenido. Elige el paquete ejecutable, no el archivo `Source code` generado por GitHub.
2. Ejecuta **Install.cmd** y elige las carpetas del programa y de datos. El instalador crea accesos directos en el escritorio y el menú Inicio. También puedes ejecutar directamente `TokenMonitor.exe` tras extraerlo.
3. Conecta los servicios que necesites desde **连接设置** (ajustes de conexión) en cada tarjeta y activa sus ventanas o la esfera.

El intervalo predeterminado es de **30 segundos**. En el panel puedes elegir 15 / 30 / 60 / 120 segundos o actualizar manualmente. Al cerrar la ventana principal, la aplicación permanece en la bandeja del sistema. Haz doble clic en su icono para abrirla o utiliza su menú para salir por completo.

[Instalación y solución de problemas](docs/START_HERE.zh-CN.md) · [Guía completa](docs/USER_GUIDE.zh-CN.md) (ambas en chino)

### DeepSeek: API Key o Harness

Introduce una API Key en **DeepSeek 密钥**, o inicia sesión en Harness con el mismo usuario de Windows y selecciona **DeepSeek 密钥 → DeepSeek 余额来源 → DeepSeek Harness 登录账户**. Guarda la selección; no es necesario mantener Harness abierto.

El modo Harness combina los saldos recargados y promocionales por moneda. Lee las credenciales desde su ubicación local predeterminada cuando las necesita, sin copiarlas al almacenamiento de Token Monitor. Puedes cambiar entre ambas fuentes. No se admiten carpetas personalizadas de Harness ni servidores no oficiales. Consulta el [tratamiento de credenciales](SECURITY.md).

### OpenAI: verificación de la clave y acceso al historial

Verifica tu API Key normal y añade después la **Admin API Key** de la organización y el **API Key ID** correspondiente (`key_…`). Verificar la clave normal no concede acceso al historial. Si la configuración está incompleta, se muestra el siguiente paso en lugar de indicar un uso de cero.

## Historial, actualizaciones y datos locales

### Tendencias de 7 y 30 días

Los gráficos utilizan la hora de Pekín (UTC+8). ChatGPT y DeepSeek guardan la última lectura correcta de cada intervalo de 15 minutos. El historial se registra por fuente local; cambiar la cuenta afecta a la curva.

Los totales diarios de OpenAI se guardan por separado mediante un hash del Key ID. Cada consulta corrige los valores diarios obtenidos del mes actual. Los meses anteriores conservan únicamente las lecturas ya recogidas; los datos de hoy son provisionales.

**Un cambio de saldo no equivale a un gasto.** Las recargas, los créditos promocionales y los reembolsos también modifican el saldo de DeepSeek.

### Actualizaciones desde la aplicación

Abre **关于软件** (acerca de) para buscar actualizaciones, leer las novedades y descargar el paquete. Se comprueban SHA-256 y la versión del ejecutable antes de pedir confirmación para reiniciar e instalar. Se conservan la configuración, las credenciales y el historial; desde la misma página puedes restaurar la versión anterior.

Se buscan versiones estables al iniciar y cada seis horas tras una comprobación correcta. Los fallos de red y los límites de GitHub no interrumpen la monitorización; sigue disponible el enlace de descarga manual. Desde v3.1.0 o anteriores, la primera actualización debe instalarse manualmente.

[Versiones publicadas](https://github.com/Ricki0613/TokenMonitor/releases) · [Registro de cambios](CHANGELOG.md)

### Tus datos se quedan en el equipo

El funcionamiento normal y el guardado de claves no requieren permisos de administrador. Los datos se guardan en `%LOCALAPPDATA%\TokenMonitor` por defecto. En **数据目录** puedes elegir una carpeta vacía con permiso de escritura: se copian y verifican la configuración, las credenciales y el historial antes de cambiar de ubicación. La carpeta original se conserva como copia de seguridad.

- Las claves de DeepSeek y OpenAI se cifran con Windows DPAPI para el usuario actual.
- Las credenciales de Harness se leen cuando hacen falta y solo se envían al servicio oficial de DeepSeek.
- Las consultas de uso no generan solicitudes de inferencia. Utilizar un restablecimiento oficial de cuota requiere una confirmación aparte.
- La búsqueda de actualizaciones no envía a GitHub las credenciales, la configuración ni el uso de la cuenta.

Más información en [SECURITY.md](SECURITY.md). Las estadísticas pueden llegar con retraso del proveedor. No hay ajuste de inicio automático y los ejecutables publicados todavía no tienen firma de código.

## Desarrollo y contribuciones

Se agradecen los [informes de errores y sugerencias](https://github.com/Ricki0613/TokenMonitor/issues/new/choose), así como las pull requests. No incluyas credenciales reales ni datos de cuenta en capturas, registros o commits. Consulta [CONTRIBUTING.md](CONTRIBUTING.md).

<details>
<summary>Compilar y verificar desde el código fuente</summary>

```powershell
git clone https://github.com/Ricki0613/TokenMonitor.git
cd TokenMonitor
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui.ps1
```

Los resultados se guardan en `build/`. Se utiliza el compilador C# de .NET Framework. Las pruebas sin conexión usan datos de ejemplo y no necesitan cuentas ni claves reales. Genera paquetes con `scripts/package.ps1` y verifica la instalación con `scripts/test-package.ps1 -Archive <ZIP>`.

La imagen del README se genera al doble de resolución desde controles WPF nativos con `scripts/render-readme.ps1` y datos de ejemplo. Para activar las notificaciones, publica una versión estable con el ZIP ejecutable y márcala como Latest; una etiqueta Git por sí sola no basta.

</details>

## Licencia

[MIT License](LICENSE) · Copyright (c) 2026 Ricki0613

Proyecto independiente, sin afiliación oficial con OpenAI ni DeepSeek.
