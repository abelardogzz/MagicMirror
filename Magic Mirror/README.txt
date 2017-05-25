ReadMe
Integrantes. Autores:
	Abelardo González 	A01195884
	Carlos Caceres 		A01195914
Aportaciones:
	Ayuda del proyecto de Kinect Skeleton Basics
	Reutilizacion de metodos.

Instrucciones para instalacion:
	Abrir solucion "Magic Mirror WPF".
	Si se desea personificar a otro personaje (que no sea el esqueleto que viene por default):  
		Mover las imagenes que se usaran del personaje a mostrar a la carpeta de "Resources"
		Desplazar estas imagenes a la carpeta Resources dentro de la carpeta principal
			**Cambiar el nombre de las imagenes a la parte del cuerpo correspondiente ( Ej. cabeza.jpg, antebrazoIzq.png, etc.)
		Dentro del "MainWindow.xaml" 
			en la seccion <Canvas>(Lista de imagenes)</Canvas>, 
				Ajustar para cada imagen su nuevo RenderTransformOrigin (si es que cambia). Debe de ponerse en el punto del joint con el que esta conectando.
	
	Si se desea personificar con el programa Default
		Correr programa	

Asegurarse de tener conectado el kinect y a la fuente de poder.
Correr el programa.

Video: https://youtu.be/owzc78hPOd4 