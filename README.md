# HxLunchBot
Bot de almuerzos hexácticos

Solución creada a partir del [template de proyecto para el SDK Bot Builder V4](https://marketplace.visualstudio.com/items?itemName=BotBuilder.BotBuilderV4)

## Pasos para correr la solución
- Abrir la solución en [Visual Studio 2017](https://visualstudio.microsoft.com/downloads/)
- Correr la solución (F5)
- Abrir el [Emulador de Bot Framework v4](https://github.com/Microsoft/BotFramework-Emulator/releases)
- Clickear el botón "Open Bot" y buscar en el filesystem la ubicación del archivo HxLunchBot.bot incluido en la solución.

## Consideraciones
- El bot está diseñado para conectarse a una base de datos [Firebase](https://console.firebase.google.com/) que responde al modelo de la aplicación (Restaurants y Votos). Será responsabilidad del usuario del código crear dicha base, o en su defecto modificar/reemplazar la clase DBClient para utilizar otro tipo de persistencia.
- El endpoint del cual levanta los datos el bot se configura como variable de entorno. Esto se observa en el constructor de la clase DBClient.
- Para correr el bot en forma local es necesario crear dicha variable en las propiedades del proyecto, solapa Debug. El nombre de la variable es "DatabaseEndpoint" y su valor debe ser la url desde la que Firebase expone los datos, ej: https://lunchbot.firebaseio.com/
- Si se publica el bot en Azure, es necesario crear dicha variable dentro del blade de Application Settings.
