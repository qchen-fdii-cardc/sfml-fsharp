// #r "nuget: SFML.Net, 2.6.0"

open System
open SFML.Graphics
open SFML.System
open SFML.Window

// Constants for configuration
let [<Literal>] WindowWidth = 800u
let [<Literal>] WindowHeight = 600u
let [<Literal>] WindowTitle = "SFML F# Example"
let [<Literal>] Boundary = 10
let [<Literal>] CirclesPerFrame = 5


let randColor (rand: Random) transparent =
    Color(
        byte (rand.Next(0, 256)),
        byte (rand.Next(0, 256)),
        byte (rand.Next(0, 256)),
        byte transparent
    )

// Creates a new random circle shape
let createRandomCircle (rand: Random) (bounds: IntRect) =
    let circleRadius = rand.Next(4, 80) |> float32
    let circle = new CircleShape(circleRadius)
    let randomColor = randColor rand 128
    circle.FillColor <- randomColor

    let x = rand.Next(bounds.Left, bounds.Left + bounds.Width - 2 * int circleRadius)
    let y = rand.Next(bounds.Top, bounds.Top + bounds.Height - 2 * int circleRadius)
    circle.Position <- Vector2f(float32 x, float32 y)
    circle

// Draws the boundary rectangle
let drawBoundary (window: RenderWindow) =
    let size = window.Size
    let rect =
        new RectangleShape(
            Vector2f(
                float32 (int size.X - 2 * Boundary),
                float32 (int size.Y - 2 * Boundary)
            )
        )
    rect.Position <- Vector2f(float32 Boundary, float32 Boundary)
    rect.OutlineColor <- Color.White
    rect.OutlineThickness <- 2.0f
    rect.FillColor <- Color.Transparent
    window.Draw(rect)

// Draws a set of random circles and adds them to a list
let addNewCircles (window: RenderWindow) (rand: Random) (circles: ResizeArray<CircleShape>) =
    let width = int window.Size.X
    let height = int window.Size.Y

    let drawingBounds =
        IntRect(
            Boundary,
            Boundary,
            width - 2 * Boundary,
            height - 2 * Boundary
        )

    for _ in 1 .. CirclesPerFrame do
        let circle = createRandomCircle rand drawingBounds
        circles.Add(circle)

[<EntryPoint>]
[<STAThread>]
let main argv =
    let videoMode = VideoMode(WindowWidth, WindowHeight)
    use window = new RenderWindow(videoMode, WindowTitle)

    let rand = Random()
    let addingCircles = ref true
    let circles = ResizeArray<CircleShape>()

    window.Closed.Add(fun _ -> window.Close())

    window.KeyPressed.Add(fun args ->
        match args.Code with
        | Keyboard.Key.Space ->
            addingCircles := not !addingCircles
            let title =
                if !addingCircles then
                    WindowTitle + " (Adding Circles)"
                else
                    WindowTitle + " (Paused)"
            window.SetTitle(title)
        | Keyboard.Key.Escape -> window.Close()
        | _ -> ()
    )
    
    while window.IsOpen do
        window.DispatchEvents()

        if !addingCircles then
            addNewCircles window rand circles

        window.Clear(Color.Transparent)

        for circle in circles do
            window.Draw(circle)

        drawBoundary window        
        
        window.Display()
        
        System.Threading.Thread.Sleep(10)

    0 // return an integer exit code