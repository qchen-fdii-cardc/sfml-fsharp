// #r "nuget: SFML.Net, 2.6.0"
// dotnet add package SFML.Net --version 2.6.0

open System
open SFML.Graphics
open SFML.System
open SFML.Window
open System.Runtime.InteropServices

module PInvoke =
    [<StructLayout(LayoutKind.Sequential)>]
    type MARGINS =
        struct
            val mutable cxLeftWidth: int
            val mutable cxRightWidth: int
            val mutable cyTopHeight: int
            val mutable cyBottomHeight: int
        end

    [<DllImport("dwmapi.dll")>]
    extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, [<In>] MARGINS& pMarInset)

    [<DllImport("user32.dll")>]
    extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags)

module Config =
    // Constants for configuration
    let [<Literal>] Boundary = 2
    let [<Literal>] CirclesPerFrame = 5
    // compare inputSequence with the Konami code
    let KonamiCode = List.rev [
        Keyboard.Key.Up
        Keyboard.Key.Up
        Keyboard.Key.Down
        Keyboard.Key.Down
        Keyboard.Key.Left
        Keyboard.Key.Right
        Keyboard.Key.Left
        Keyboard.Key.Right
        Keyboard.Key.B
        Keyboard.Key.A
    ]
    // Constants for SetWindowPos
    let HWND_TOPMOST = IntPtr(-1)
    let SWP_NOMOVE = 0x0002u
    let SWP_NOSIZE = 0x0001u

module Circle =
    let private randColor (rand: Random) transparent =
        Color(
            byte (rand.Next(100, 256)),
            byte (rand.Next(100, 256)),
            byte (rand.Next(100, 256)),
            byte (rand.Next(100, transparent))
        )

    // Creates a new random circle shape
    let createRandom (rand: Random) (bounds: IntRect) =
        let circleRadius = rand.Next(4, 80) |> float32
        let circle = new CircleShape(circleRadius)
        let randomColor = randColor rand 128
        circle.FillColor <- randomColor

        let x = rand.Next(bounds.Left, bounds.Left + bounds.Width - 2 * int circleRadius)
        let y = rand.Next(bounds.Top, bounds.Top + bounds.Height - 2 * int circleRadius)
        circle.Position <- Vector2f(float32 x, float32 y)
        circle

    // Draws a set of random circles and adds them to a list
    let addNews (window: RenderWindow) (rand: Random) (circles: ResizeArray<CircleShape>) =
        let width = int window.Size.X
        let height = int window.Size.Y

        let drawingBounds =
            IntRect(
                Config.Boundary,
                Config.Boundary,
                width - 2 * Config.Boundary,
                height - 2 * Config.Boundary
            )

        for _ in 1 .. Config.CirclesPerFrame do
            let circle = createRandom rand drawingBounds
            circles.Add(circle)

    let isOutOfBounds (circle: CircleShape) (bounds: IntRect) =
        let pos = circle.Position
        let radius = circle.Radius

        if pos.X < float32 bounds.Left || pos.X + 2.f * radius > float32 (bounds.Left + bounds.Width) ||
           pos.Y < float32 bounds.Top || pos.Y + 2.f * radius > float32 (bounds.Top + bounds.Height) then
            true
        else
            false

    let applyGravity (circle: CircleShape) =
        let pos = circle.Position
        let radius = circle.Radius
        let center = Vector2f(pos.X + radius, pos.Y + radius)

        // Define a constant upward velocity
        let vel = 0.1f * radius + 1.0f

        // Update radius and center position
        let newRadius = radius * 1.005f
        let newCenter = Vector2f(center.X, center.Y - vel)

        // Recalculate the top-left position based on the new center and radius
        circle.Radius <- newRadius
        circle.Position <- Vector2f(newCenter.X - newRadius, newCenter.Y - newRadius)

module Window =
    // Draws the boundary rectangle
    let drawBoundary (window: RenderWindow) =
        let size = window.Size
        let rect =
            new RectangleShape(
                Vector2f(
                    float32 (int size.X - 2 * Config.Boundary),
                    float32 (int size.Y - 2 * Config.Boundary)
                )
            )
        rect.Position <- Vector2f(float32 Config.Boundary, float32 Config.Boundary)
        rect.OutlineColor <- Color.Red
        rect.OutlineThickness <- 2.0f
        rect.FillColor <- Color.Transparent
        window.Draw(rect)

    let makeTransparentAndTopmost (window: RenderWindow) =
        // Make the window transparent
        let mutable margins = PInvoke.MARGINS()
        margins.cxLeftWidth <- -1
        margins.cxRightWidth <- -1
        margins.cyTopHeight <- -1
        margins.cyBottomHeight <- -1
        PInvoke.DwmExtendFrameIntoClientArea(window.SystemHandle, &margins)
        // Make the window topmost
        PInvoke.SetWindowPos(window.SystemHandle, Config.HWND_TOPMOST, 0, 0, 0, 0, Config.SWP_NOMOVE ||| Config.SWP_NOSIZE) |> ignore

[<EntryPoint>]
[<STAThread>]
let main argv =
    let videoMode = VideoMode.DesktopMode
    use window = new RenderWindow(videoMode, "", Styles.None)
    
    Window.makeTransparentAndTopmost window

    let rand = Random()
    let addingCircles = ref true
    let circles = ResizeArray<CircleShape>()

    // The Closed event is intentionally not handled to block Alt+F4.
    // window.Closed.Add(fun _ -> window.Close())

    let mutable inputSequence = []

    window.KeyPressed.Add(fun args ->
        inputSequence <- args.Code :: inputSequence
        
        match args.Code with
        | Keyboard.Key.Space ->
            addingCircles := not !addingCircles
        | Keyboard.Key.Escape ->           
            circles.Clear()
            inputSequence <- []
        | _ -> ()

        if inputSequence = Config.KonamiCode then
            window.Close()
    )
    
    while window.IsOpen do
        window.DispatchEvents()

        if !addingCircles then
            Circle.addNews window rand circles

        window.Clear(Color.Transparent)
        let mutable toRemove = []
        for circle in circles do
            Circle.applyGravity circle
            let bounds =
                IntRect(
                    Config.Boundary,
                    Config.Boundary,
                    int window.Size.X - 2 * Config.Boundary,
                    int window.Size.Y - 2 * Config.Boundary
                )
            if Circle.isOutOfBounds circle bounds then
                toRemove <- circle :: toRemove
            else    
                window.Draw(circle)
        for circle in toRemove do
            circles.Remove(circle) |> ignore

        Window.drawBoundary window        
        
        window.Display()
        
        System.Threading.Thread.Sleep(10)

    0 // return an integer exit code