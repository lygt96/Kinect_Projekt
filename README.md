Readme-Todo:
* Bilder einfügen
* weitere Details

# Erstes POC Projekt - C# mit Raspberry Pi 4
Dieses Projekt soll zeigen, welche Möglichkeiten sich bieten, eine Computer Vision Applikation auf einem Raspberry Pi auszuführen, welches in C# geschrieben wurde.

## Der Raspberry Pi
### Technische Spezifikationen
Hier handelt es sich um die neuste Version des Einplatinen-Computers: dem **Raspberry Pi 4B**. Die wichtigsten technischen Spezifikationen lauten:

* Broadcom BCM2711, Quad core Cortex-A72 (ARM v8) 64-bit SoC @ 1.5GHz
* 4GB Arbeitsspeicher
* 2.4 GHz und 5.0 GHz IEEE 802.11ac wireless, Bluetooth 5.0, BLE
* Gigabit Ethernet
* 2 USB 3.0 ports; 2 USB 2.0 ports.
* Raspberry Pi standard 40 Pin GPIO Header
* 2 × Micro-HDMI Ports (bis zu 4K bei 60FPS)
* 2-lane MIPI DSI Displayport
* 2-lane MIPI CSI Kameraport
* 4-pole Stereoaudio und Compositevideo Port
* H.265 (4kp60 decode), H264 (1080p60 decode, 1080p30 encode)
* OpenGL ES 3.0 Graphics
* Micro-SD Slot
* 5V DC via USB-C

Die GPIOs sind vielseitig einsetzbar - einige haben bspw. eine Hardware PWM Funktion, andere sind I²C-fähig. Weiter lässt sich der GPIO Header einsetzen zum Verbinden von [verschiedenen HAT Modulen](https://uk.pi-supply.com/products/iot-lora-node-phat-for-raspberry-pi).

### Das Betriebssystem
Auf dem Pi 4B läuft die neuste Raspian Buster, welches auf Debian Buster (also Debian 10) basiert. Dieses Betriebssystem liefert ein "Windows-ähnliches"
Erlebnis im Sinne von Explorer, Desktop, Start-Menü uvm.

Die IoT Version von Windows, das sog. "Windows 10 IoT" läuft **nicht** auf dieser Version vom Pi. Auch auf dem Pi 2 und Pi 3 ist dieses Betriebssystem
nicht offiziell unterstützt, allerdings mit dem [Installer von Windows](https://docs.microsoft.com/de-de/windows/iot-core/downloads) installierbar.

## Das Projekt
### Das Ziel
Demonstriert werden soll, wie man eine CV-Applikation, die in C# geschrieben ist auf dem Pi ausführen kann. Dafür wird hier
als Kamera das Kamerasystem der **Xbox 360 Kinect** verwendet.

![](https://static-de.gamestop.de/images/products/235803/3max.jpg "Kinect v1")

Das Kamerasystem beinhaltet eine RGB-Kamera und ein System zur Tiefenerkennung basierend auf dem Structured-Light-Prinzip. 
Hierbei wird die Kinect mittels eines USB Steckers an den Pi verbunden - und nicht etwa über den MIPI CSI Port. 
Ohne auf die Details der Funktionsweise der Kinect, sowie der Software welches die Kinect ansteuert einzugehen, 
sollen die Daten die von der Kinect erhalten werden in Bilder umgewandelt werden.
Desweiteren, sollen die Daten des Beschleinigungssensors des Kinects auf einem 1602a LCD Display ausgegeben werden, 
welches über die GPIO Ports des Pis verbunden ist. Dafür wird die Software aus meiner 
Repo [lygt96/1602a_LCD](https://github.com/lygt96/1602a_LCD) verwendet, welches ebenfalls in C# geschrieben wurde.

### Die Entwicklungsumgebung
Als IDE wird Visual Studio 2019 auf einem Windows 10 Rechner verwendet. Kompilieren geschieht ebenfalls auf dem IDE-Rechner. 
Mittels FTP sowie SSH kann die Applikation auf den Pi übertragen und ausgeführt werden.
Die in Visual Studio eingebaute Remote-Debug Funktion erleichtert das "entfernte Programmieren" mit dem Pi.
![](https://i.imgur.com/aJBfqLc.png "Debug")

### Vorbereitungen und Dependencies
In diesem Projekt müssen allgemein 3 Vorbereitungen bezüglich des Codes getroffen werden:
* die Bibliothek zum Ansteuern der GPIOs
* die Bibliothek zum Ansteuern des Kinects
* die Bibliothek für Computer Vision

#### Ansteuern der GPIOs
Die Details zu dieser Bibliothek können der Repo [lygt96/1602a_LCD](https://github.com/lygt96/1602a_LCD) entnommen werden.

#### Ansteuern des Kinects
OpenKinect ermöglicht es, auch mit C# an eine Linux Maschine angeschlossene
Kinect zu steuern. Die benötigten Quelldaten können der Website entnommen
und unter Windows kompiliert werden, um in Visual Studio eine Referenz zu dieser Bibliothek
zu erstellen. Der Prozess von der Initialisierung des Kinects bis hin zum
erhalten eines Bytearrays mit den Bilddaten, ist im Code erklärt. 

#### Computer Vision
In C# lässt sich Computervision mithilfe von EmguCV realisieren. Diese Bibliothek basiert
auf der originalen OpenCV Bibiliothek für C++ und funktioniert identisch. Die Bibliothek muss
unter Linux kompiliert werden, da sie nicht von Visual Studio gemaneged wird. Die jeweiligen Librarys müssen 
sich daher stets im gleichen Verzeichnis wie die Executable befinden.

### Funktionsweise
Beim Starten der Applikation werden zunächst alle Komponenten, d.h.
das LCD Display und die Kinect initialisiert. Im Anschluss
steht es dem Benutzer frei, einen Test des Servomotors des Kinects durchzuführen.
Anschließend können für die Farb- sowie für die Tiefenkamera die jeweiligen Modi eingestellt werden.


Ist dies geschehen, arbeitet das Programm in einer Endlosschleife und wartet darauf, dass der Thread der extra
für das Verarbeiten der Daten aus dem Kinect verantwortlich ist, Daten liefert. 


```csharp

// In Main: Erstellen eines Threads
updateThread = new Thread(delegate ()
{
    try
    {
        kinect.UpdateStatus();
        Kinect.ProcessEvents();
    }
    catch (ThreadInterruptedException e)
    {
        return;
    }
    catch (Exception ex)
    {

    }
});
updateThread.Start();
```

```csharp
// Kinect initialisieren
public static void Init(Kinect kinect)
{ 
    if (Kinect.DeviceCount > 0)
    {
        // Kinect Verbinden
        kinect.Open();

        // Eventhandler
        kinect.VideoCamera.DataReceived += HandleKinectVideoCameraDataReceived;
        kinect.DepthCamera.DataReceived += HandleKinectDepthCameraDataReceived;

        // Kameras starten
        kinect.VideoCamera.Start();
        kinect.DepthCamera.Start();

        // LED Farbe ändern
        kinect.LED.Color = LEDColor.Yellow;
    }
}
```

```csharp
// Handeln der Daten der Videokamera
private static void HandleKinectVideoCameraDataReceived(object sender, BaseCamera.DataReceivedEventArgs e)
{
    // Argument e beinhaltet die Daten
    Console.WriteLine("Neue RGB-Daten @ {0}", e.Timestamp);

    // Erstelle ein EmguCV Image im RGB Modus mit den Dimensionen von e
    Image<Rgb, byte> rgbImage = new Image<Rgb, byte>(e.Data.Width, e.Data.Height);

    // Kopiere die Daten aus e in das eben erstellte Image
    rgbImage.Bytes = e.Data.Data;

    // Speichern
    rgbImage.Save("rgb_img_" + System.DateTime.Now.ToString().Replace(".", "").Replace(":", "").Replace(" ", "") + ".png");
}
```

## Roadmap
* Bessere Dokumentation
* Livepreview
* Direktes Steuern des Servos
* Streamen auf eine lokal gehostete Website, welches als Dashboard dient
* Interpretieren der Daten aus der Tiefenkamera
* Live Anzeige der nächsten und weitesten Stelle im Bild anhand Daten aus der Tiefenkamera
