using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Timers;
using static GraphicCore.Core;

//TODO:
//  rework buttons lines - input variable specifying if same size equally divided full size, same size specified size centered, same size specified size distributed thorough full size, margin size centered, margin size distributed thorough full size

namespace GraphicCore
{
    internal class Core
    {
        //global variables
        public static ConsoleColor Foreground, Background, ButtonFore, ButtonBack, HighFore, HighBack;
        public static int PlannedWidth, PlannedHeight, MinWidth, MinHeight, Priorized;
        public static string Frame = "";
        public static bool TextIn, MovementLock = true;
        public static int DebugPriority, PopUpPriority, MenuPriority;
        public static Button?[,] ButtonField = new Button[0,0];
        static Dictionary<string, Action<DisplayObject, string>> DebugCommands = new()
        {
            {"exit", (x,y) => { Environment.Exit(0); } },
            {"adjust", (x, y) => {/*WIP*/ } },
            {"info", (x, y) => { CleanFrame();
                        CursorPosition(1, 1);
                        Console.Write("Press enter to exit\n|-------------------\n");
                        Console.WriteLine($"|Console width/heigth: {Console.WindowWidth}/{Console.WindowHeight}\n|Planned width/height: {PlannedWidth}/{PlannedHeight}\n");
                        Console.WriteLine($"|Colors bg/fg: {Background}/{Foreground}");
                        Console.WriteLine($"|Ram usage: {GC.GetTotalMemory(false)} B");
                        Console.WriteLine($"|Frame: {x.frameType}");
                        KeyInput(-20);
                        x.Draw();} },
            {"How to disappear?", (x, y) => {
                        Disappear();
                        KeyInput(-20);;
                        x.Draw(); } },
            {"fore", (x, y) => {
                try
                {
                    ConsoleColor inputColor;
                    y = y[0].ToString().ToUpper() + y.Substring(1);
                    if (Enum.TryParse(y, out inputColor))
                    {
                        Foreground = inputColor;
                    }
                    x.Draw();
                }
                catch (Exception)
                {
                }} },
            {"back", (x, y) => {
                try
                {
                    ConsoleColor inputColor;
                    y = y[0].ToString().ToUpper() + y.Substring(1);
                    if (Enum.TryParse(y, out inputColor))
                    {
                        Background = inputColor;
                    }
                    x.Draw();
                }
                catch (Exception)
                {
                } } },
            {"bfore", (x, y) => {
                try
                    {
                        ConsoleColor inputColor;
                        y = y[0].ToString().ToUpper() + y.Substring(1);
                        if (Enum.TryParse(y, out inputColor))
                        {
                            ButtonFore = inputColor;
                        }
                        x.Draw();
                    }
                    catch (Exception)
                    {
                    }
                } },
            {"bback", (x, y) => {
                try
                        {
                            ConsoleColor inputColor;
                            y = y[0].ToString().ToUpper() + y.Substring(1);
                            if (Enum.TryParse(y, out inputColor))
                            {
                                ButtonBack = inputColor;
                            }
                            x.Draw();
                        }
                        catch (Exception)
                        {
                        }
                } },
            {"", (x, y) => { x.Draw(); } }
        };

        public static Channel<ConsoleKeyInfo> keyTunnel = Channel.CreateUnbounded<ConsoleKeyInfo>();
        /// <summary>
        /// Property MoverTask - you can add your own movement handler. Please, refer to the original Mover Task to see the requirements.
        /// </summary>
        public class Debugger
        {

            //debugger variables
            DisplayObject displayObject;
            ConsoleKeyInfo pressedKey;
            string[] input;
            StringBox box;
            Task keyTask, moverTask;
            public Task MoverTask{ set { moverTask.Dispose(); moverTask = value; moverTask.Start(); } }

            //debugger properties
            public DisplayObject DisplayObject
            {
                get { return displayObject; }
                set { displayObject = value; }
            }
            public Debugger(ConsoleColor foreground, ConsoleColor background, ConsoleColor buttonFore, ConsoleColor buttonBack, ConsoleColor highFore, ConsoleColor highBack, int minWidth, int minHeight, int priorized = int.MaxValue,int debugPriority = 0, int popUpPriority = 2, int menuPriority = 6)
            {
                displayObject = new DisplayObject();
                Foreground = foreground;
                Background = background;
                ButtonFore = buttonFore;
                ButtonBack = buttonBack;
                HighFore = highFore;
                HighBack = highBack;
                PlannedWidth = MinWidth = minWidth;
                PlannedHeight = MinHeight = minHeight;
                Priorized = priorized;
                keyTask = new Task(async () =>
                {
                    await Runner();
                });
                moverTask = new Task(async () =>
                {
                    await Mover();
                });
                keyTask.Start();
                moverTask.Start();
                displayObject.Adjust();
                Frame = createFrame();
                TextIn = false;
                DebugPriority = debugPriority;
                PopUpPriority = popUpPriority;
                MenuPriority = menuPriority;
                box = new StringBox(PlannedWidth-25, "Debug command:", 0, 0, 0, 0, DebugPriority, true);
                input = new string[0];
            }
            public async Task Runner()
            {
                while (true)
                {
                    pressedKey = ConsoleKeyInput();
                    if (pressedKey.Modifiers == ConsoleModifiers.Control && pressedKey.Key == ConsoleKey.D && Priorized != 0)
                    {
                        new Task(() => { DbConsole(null); }).Start();
                    }
                    else
                    {
                        await keyTunnel.Writer.WriteAsync(pressedKey);
                    }
                }
            }
            public async Task Writer()
            {
                while (await keyTunnel.Reader.WaitToReadAsync())
                {
                    while (keyTunnel.Reader.TryRead(out pressedKey) && !TextIn && Priorized != 0)
                    {
                        if (pressedKey.Key == ConsoleKey.Enter)
                        {
                            displayObject.ButtonClick();
                        }
                        else
                        {
                            displayObject.ButtonChange(displayObject.PositionCalc(pressedKey));
                        }
                    }
                }
            }
            /// <summary>
            /// Important: the conditions of the outer and inner while loop should be the same!
            /// </summary>
            /// <remarks>
            /// Reader conditions - provide looping through the channel and reading.<br/>
            /// TextIn condition - prevents movement while text is being written.<br/>
            /// MovementLock condition - prevents movement when there's a static overlay, program is waiting for a response, etc.<br/>
            /// Priorized - priority of the debugger. If 0, the debugger is currently used.
            /// </remarks>
            public async Task Mover()
            {
                while (await keyTunnel.Reader.WaitToReadAsync())
                {
                    while (keyTunnel.Reader.TryRead(out pressedKey) && !TextIn && !MovementLock && Priorized != DebugPriority)
                    {
                        if (pressedKey.Key == ConsoleKey.Enter)
                        {
                            displayObject.ButtonClick();
                        }
                        else
                        {
                            displayObject.ButtonChange(displayObject.PositionCalc(pressedKey));
                        }
                    }
                }
            }
            public void DbConsole(string? command)
            {
                int originalPriorized = Priorized;
                if (command != null)
                {
                    input = command.Split(':');
                }
                else
                {
                    Priorized = 0;
                    input = box.Get(ref keyTunnel, "", false, this, true, false).Split(':');
                }
                if (input.Length == 0) { return; }
                #region Commands
                Action<DisplayObject, string>? action;
                if (DebugCommands.TryGetValue(input[0], out action)) 
                {
                    if (input.Length == 1)
                    {
                        input = input.Append("").ToArray();
                    }
                    action.Invoke(displayObject, input[1]);
                } else
                {
                    CursorPosition(19, 1);
                    ColorString($"{input[0]} is not a command");
                    Thread.Sleep(1700);
                    displayObject.Draw();
                }
                #endregion
                keyTunnel.Reader.ReadAllAsync();
                if (command == null)
                {
                    Priorized = originalPriorized;
                }
            }
            // Console Key Input, reads key presses and returns them
            public ConsoleKeyInfo ConsoleKeyInput()
            {
                while (true)
                {
                    ConsoleKeyInfo pressedKey = Console.ReadKey(true);
                    return pressedKey;
                }
            }
        }
        // Object for storing and displaying data
        public class DisplayObject
        {
            //frameType type of displayed information
            // -1 - Adjustment
            // 0 - Menu - Centered
            // 1 - Defined by program
            // 2 - Popup
            // 3 - Title screen - Timed
            // 4 - Title screen - Static
            //hlIndex - index of highlighted Button
            // DisplayObject draws all the data from Data classes. In case other objects need to be drawn, must handle individually
            public int frameType;
            /// <summary>
            /// hlIndex - WARNING - first index is y, second is x. made such way because i didnt think forward enough;
            /// </summary>
            public Tuple<int, int> hlIndex;
            public IData frame;
            public Stack<IData> frameHistory = new();
            public DisplayObject()
            {
                frameType = -1;
                hlIndex = Tuple.Create(0, 0);
                frame = new TitleData(new string[0], 0, new ConsoleColor?[0], new ConsoleColor?[0]);
            }
            ///// <summary>
            ///// </summary>
            ///// <param name = "frame" >
            ///// frameType type of displayed information<br/>
            ///// -1 - Adjustment<br/>
            ///// 0 - Menu - Centered<br/>
            ///// 1 - Defined by program<br/>
            ///// 2 - Popup<br/>
            ///// 3 - Title screen - Timed<br/>
            ///// 4 - Title screen - Static<br/></param>

            /// <summary>
            /// Draws current frame
            /// </summary>
            public void Draw()
            {
                if(frame is not null) { frame.Write(); } else { throw new Exception("Empty frame variable."); }
            }

            /// <summary>
            /// Draws a new frame. Old is forgotten.
            /// </summary>
            /// <param name="delOld">True when old frame should disappear.</param>
            /// <param name="frame">IData object containing the new frame.</param>
            public void DrawNew(IData frame, bool delOld = true)
            {
                this.frame.Delete(delOld);
                this.frame = frame;
                this.frame.Write();
                if(FindIndex()) { Highlight(); }
            }
            /// <summary>
            /// Draws new frame. Old added to history (Stack).
            /// </summary>
            /// <param name="delOld">True when old frame should disappear.</param>
            /// <param name="frame">IData object containing the new frame.</param>
            public void DrawNewOver(IData frame, bool delOld = true)
            {
                this.frame.Delete(delOld);
                frameHistory.Push(this.frame);
                this.frame = frame;
                frame.Write();
                if (FindIndex()) { Highlight(); }
            }

            /// <summary>
            /// Draws last item from history. Keeps current if no history
            /// </summary>
            /// <param name="delOld">True when old frame should disappear.</param>
            public void DrawOld(bool delOld = true)
            {
                if(frameHistory.Count != 0)
                {
                    frame.Delete(delOld);
                    frame = frameHistory.Pop();
                    frame.Write();
                    if (FindIndex()) { Highlight(); }
                }
            }

            //public void Draw(int? frame = null)
            //{
            //    ClearOld(frame ?? -100);
            //    frameType = frame == null ? frameType : frame.Value;
            //    try
            //    {
            //        switch (frameType)
            //        {
            //            case 0:
            //                if (menuData != null)
            //                {
            //                    menuData.Write();
            //                    if (ButtonField[hlIndex.Item1, hlIndex.Item2] == null)
            //                    {
            //                        if (!FindIndex())
            //                        {
            //                            return;
            //                        }
            //                    }
            //                    ButtonField[hlIndex.Item1, hlIndex.Item2].Highlight();
            //                }
            //                else
            //                {
            //                    throw new Exception("MenuData not initialized");
            //                }
            //                break;
            //            case 2:
            //                if (popupData != null)
            //                {
            //                    popupData.Write();
            //                    if (popupData.buttons.Count != 0)
            //                    {
            //                        if (ButtonField[hlIndex.Item1, hlIndex.Item2] == null)
            //                        {
            //                            if (!FindIndex())
            //                            {
            //                                throw new Exception("No buttons in popup");
            //                            }
            //                        }
            //                        ButtonField[hlIndex.Item1, hlIndex.Item2].Highlight();
            //                    }
            //                }
            //                else
            //                {
            //                    throw new Exception("PopupData not initialized");
            //                }
            //                break;
            //            case 3:
            //                CleanFrame();
            //                if (titleData != null)
            //                {
            //                    titleData.WriteTimed();
            //                }
            //                else
            //                {
            //                    throw new Exception("TitleData not initialized");
            //                }
            //                break;
            //            case 4:
            //                CleanFrame();
            //                if (titleData != null)
            //                {
            //                    titleData.Write();
            //                }
            //                else
            //                {
            //                    throw new Exception("TitleData not initialized");
            //                }
            //                break;
            //            default:
            //                throw new Exception("Wrong or none frameType");
            //        }
            //    }
            //    catch (Exception)
            //    {
            //        Console.Clear();
            //    }
            //}

            //Adjust screen
            public void Adjust() 
            {
                ConsoleKeyInfo pressedKey = new ConsoleKeyInfo('M', ConsoleKey.M, true, false, false);
                Task resetter = new Task(() =>
                {
                    while (frameType == -1)
                    {
                        CleanFrame();
                        CursorPosition((Console.WindowWidth - 114) / 2, (Console.WindowHeight - 7) / 2);
                        Console.Write("For the best experience, resize the console window and/or the text so the crosses sit in corners and form a frame.");
                        CursorPosition((Console.WindowWidth - 23) / 2, ((Console.WindowHeight - 7) / 2) + 2);
                        Console.Write("Press ");
                        new PosString("Enter", Console.CursorLeft, Console.CursorTop, null, ConsoleColor.Green).Write();
                        Console.Write(" to confirm.");
                        CursorPosition((Console.WindowWidth - 27) / 2, ((Console.WindowHeight - 7) / 2) + 4);
                        Console.Write("(Use ");
                        new PosString("Scroll", Console.CursorLeft, Console.CursorTop, null, ConsoleColor.Cyan).Write();
                        Console.Write(" to resize font)");
                        CursorPosition((Console.WindowWidth - 46) / 2, ((Console.WindowHeight - 7) / 2) + 6);
                        Console.Write("Press ");
                        new PosString("R", Console.CursorLeft, Console.CursorTop, null, ConsoleColor.Cyan).Write();
                        Console.Write(" to resize to your current window size.");
                        PlannedHeight = Console.WindowHeight < MinHeight ? MinHeight : Console.WindowHeight;
                        PlannedWidth = Console.WindowWidth < MinWidth ? MinWidth : Console.WindowWidth;
                        Frame = createFrame();
                        Thread.Sleep(250);
                    }
                });
                resetter.Start();
                while (true)
                {
                    pressedKey = KeyInput(DebugPriority - 1, 0, 0, false);
                    if (pressedKey.Key == ConsoleKey.Enter)
                    {
                        frameType = 0;
                        PlannedHeight = Console.WindowHeight < MinHeight ? MinHeight : Console.WindowHeight;
                        PlannedWidth = Console.WindowWidth < MinWidth ? MinWidth : Console.WindowWidth;
                        Frame = createFrame();
                        ButtonField = new Button[PlannedHeight, PlannedWidth];
                        break;
                    }
                    else if (pressedKey.Key == ConsoleKey.R)
                    {
                        PlannedHeight = Console.WindowHeight < MinHeight ? MinHeight : Console.WindowHeight;
                        PlannedWidth = Console.WindowWidth < MinWidth ? MinWidth : Console.WindowWidth;
                        Frame = createFrame();
                    }
                }
            }
            #region Draw methods
            // Writes old button normally and new button highlighted
            public void ButtonChange(Tuple<int, int> newIndex)
            {
                try
                {
                    if (ButtonField[hlIndex.Item1, hlIndex.Item2] != null)
                    {
                        ButtonField[hlIndex.Item1, hlIndex.Item2].Write();
                    }
                    hlIndex = newIndex;
                    ButtonField[hlIndex.Item1, hlIndex.Item2].Highlight();
                }
                catch (Exception)
                {
                    Console.Clear();
                }

            }
            // buttonField based highlight index movement calculations
            // Returns coordinates of the centre of the new highlighted button
            public Tuple<int, int> PositionCalc(ConsoleKeyInfo keyMovement)
            {
                // origin is the index of the textButton that is currently highlighted
                int posy = hlIndex.Item1, posx = hlIndex.Item2;
                // Switch for movement
                // newButtonLine is a string that is used to determine the end of a
                try
                {
                    switch (keyMovement)
                    {
                        // Movement up
                        case ConsoleKeyInfo key when keyMovement.Key == ConsoleKey.UpArrow:
                            while (posy != 0)
                            {
                                posy--;
                                int change = 0;
                                posx = hlIndex.Item2;
                                while (true)
                                {
                                    change++;
                                    if (posx - change < 0)
                                    {
                                        posx = 0;
                                        while (posx < ButtonField.GetLength(1))
                                        {
                                            if (ButtonField[posy, posx] != null)
                                            {
                                                return Tuple.Create(posy, ButtonField[posy, posx].posx + ButtonField[posy, posx].size / 2);
                                            }
                                            posx++;
                                        }
                                        break;
                                    }
                                    else if (ButtonField[posy, posx - change] != null)
                                    {
                                        return Tuple.Create(posy, ButtonField[posy, posx - change].posx + ButtonField[posy, posx - change].size / 2);
                                    }
                                    change++;
                                    if (posx + change >= ButtonField.GetLength(1))
                                    {
                                        posx = ButtonField.GetLength(1) - 1;
                                        while (posx >= 0)
                                        {
                                            if (ButtonField[posy, posx] != null)
                                            {
                                                return Tuple.Create(posy, ButtonField[posy, posx].posx + ButtonField[posy, posx].size / 2);
                                            }
                                            posx--;
                                        }
                                        break; ;
                                    }
                                    else if (ButtonField[posy, posx + change] != null)
                                    {
                                        return Tuple.Create(posy, ButtonField[posy, posx + change].posx + ButtonField[posy, posx + change].size / 2);
                                    }
                                }
                            }
                            return hlIndex;
                        // Movement down
                        case ConsoleKeyInfo key when keyMovement.Key == ConsoleKey.DownArrow:
                            while (posy != ButtonField.GetLength(0))
                            {
                                posy++;
                                int change = 0;
                                posx = hlIndex.Item2;
                                while (true)
                                {
                                    change++;
                                    if (posx - change < 0)
                                    {
                                        posx= 0;
                                        while (posx < ButtonField.GetLength(1))
                                        {
                                            if (ButtonField[posy, posx] != null)
                                            {
                                                return Tuple.Create(posy, ButtonField[posy, posx].posx + ButtonField[posy, posx].size / 2);
                                            }
                                            posx++;
                                        }
                                        break;
                                    }
                                    else if (ButtonField[posy, posx - change] != null)
                                    {
                                        return Tuple.Create(posy, ButtonField[posy, posx - change].posx + ButtonField[posy, posx - change].size / 2);
                                    }
                                    change++;
                                    if (posx + change >= ButtonField.GetLength(1))
                                    {
                                        posx= ButtonField.GetLength(1) - 1;
                                        while (posx >= 0)
                                        {
                                            if (ButtonField[posy, posx] != null)
                                            {
                                                return Tuple.Create(posy, ButtonField[posy, posx].posx + ButtonField[posy, posx].size / 2);
                                            }
                                            posx--;
                                        }
                                        break;
                                    }
                                    else if (ButtonField[posy, posx + change] != null)
                                    {
                                        return Tuple.Create(posy, ButtonField[posy, posx + change].posx + ButtonField[posy, posx + change].size / 2);
                                    }
                                }
                            }
                            return hlIndex;
                        // Movement left
                        case ConsoleKeyInfo key when keyMovement.Key == ConsoleKey.LeftArrow:
                            while (ButtonField[posy, posx] != null)
                            {
                                posx--;
                                if (posx == 0)
                                {
                                    return hlIndex;
                                }
                            }
                            while (ButtonField[posy, posx] == null)
                            {
                                posx--;
                                if (posx == 0)
                                {
                                    return hlIndex;
                                }
                            }
                            return Tuple.Create(posy, ButtonField[posy, posx].posx + ButtonField[posy, posx].size / 2);
                        // Movement right
                        case ConsoleKeyInfo key when keyMovement.Key == ConsoleKey.RightArrow:
                            while (ButtonField[posy, posx] != null)
                            {
                                posx++;
                                if (posx == ButtonField.GetLength(1))
                                {
                                    return hlIndex;
                                }
                            }
                            while (ButtonField[posy, posx] == null)
                            {
                                posx++;
                                if (posx == ButtonField.GetLength(1))
                                {
                                    return hlIndex;
                                }
                            }
                            return Tuple.Create(posy, ButtonField[posy, posx].posx + ButtonField[posy, posx].size / 2);
                        // Default
                        default:
                            Exception e = new Exception("Wrong key pressed");
                            throw e;
                    }
                }
                catch (Exception e)
                {

                    return hlIndex;
                }
            }
            //activates button 
            public void ButtonClick()
            {
                if (ButtonField[hlIndex.Item1, hlIndex.Item2] != null)
                {
                    ButtonField[hlIndex.Item1, hlIndex.Item2].Press();
                }
            }

            /// <summary>
            /// Scans left to right, top to bottom for the first clickable object and sets hlIndex to its center.
            /// </summary>
            /// <returns>True if found, false if no clickable object.</returns>
            public bool FindIndex()
            {
                for(int y = 0; y < ButtonField.GetLength(0); y++)
                {
                    for(int x = 0; x < ButtonField.GetLength(1); x++)
                    {
                        if (ButtonField[y, x] != null)
                        {
                            hlIndex = Tuple.Create(y, ButtonField[y,x].size/2 + x);
                            return true;
                        }
                    }
                }
                return false;
            }

            /// <summary>
            /// Highlights current hlIndex.
            /// </summary>
            /// <returns>True when successfully highlighted.</returns>
            public bool Highlight()
            {
                try
                {
                    ButtonField[hlIndex.Item1, hlIndex.Item2].Highlight();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            #endregion
        }
        #region displayData classes
        // TitleData - An array of positioned strings that are centered, integer data
        public class TitleData : IData
        {
            public PosString[] content;
            public int delay;
            string type = "title";
            public string Type
            {
                get { return type; }
            }

            public TitleData(string[] lines, int delay, ConsoleColor?[] fores, ConsoleColor?[] backs)
            {
                content = new PosString[0];
                this.delay = delay;
                int y = (PlannedHeight - lines.Length * 2 + 1) / 2;
                if (fores == null) fores = new ConsoleColor?[0];
                if (backs == null) backs = new ConsoleColor?[0];
                while (fores.Length < lines.Length)
                {
                    fores = fores.Append(null).ToArray();
                }
                while (backs.Length < lines.Length)
                {
                    backs = backs.Append(null).ToArray();
                }
                for (int i = 0; i < lines.Length; i++)
                {
                    content = content.Append(new PosString(lines[i], (PlannedWidth - lines[i].Length) / 2, y, null, fores[i], backs[i])).ToArray();
                    y += 2;
                }
            }
            public void Write()
            {
                CleanFrame();
                foreach (PosString item in content)
                {
                    item.Write();
                }
                Thread.Sleep(delay);
            }
            public void Delete(bool clear = true)
            {
                if (clear)
                {
                    foreach (PosString item in content)
                    {
                        item.Delete();
                    }
                }
                return;
            }
        }
        // Classical, simple menu
        public class MenuData : IData
        {
            public PosString header, headerUnderline, title;
            public List<PosString> content = new();
            public List<Button> buttons;
            string type = "menu";
            public string Type
            {
                get { return type; }
            }
            public MenuData(string header, string title, List<Button> buttons)
            {
                this.header = new PosString(header, (PlannedWidth - header.Length) / 2, 3);
                this.headerUnderline = new PosString(new string('-', header.Length), (PlannedWidth - header.Length) / 2, 4);
                this.title = new PosString(title, (PlannedWidth - title.Length) / 2, 7);
                this.buttons = buttons;
            }
            public MenuData(PosString header, PosString title, List<Button> buttons)
            {
                this.header = header;
                this.headerUnderline = new PosString(new string('-', header.Content.Length), (PlannedWidth - header.Content.Length) / 2, 4);
                this.title = title;
                this.buttons = buttons;
            }

            public void Write()
            {
                CleanFrame();
                header.Write();
                headerUnderline.Write();
                title.Write();
                foreach (Button x in buttons)
                {
                    x.Write();
                }
                foreach(PosString x in content)
                {
                    x.Write();
                }
            }
            /// <summary>
            /// Clear says whether to overwrite text with background color or not
            /// </summary>
            public void Delete(bool clear = true)
            {
                if (clear)
                {
                    header.Delete();
                    title.Delete();
                    headerUnderline.Delete();
                    foreach (PosString x in content)
                    {
                        x.Delete();
                    }
                }
                foreach (Button x in buttons)
                {
                    x.Delete(clear);
                }
            }
        }
        // popup in front of other stuff
        /// <summary>
        /// Creates a view that can take up only a part of the screen. Can be used for popups, settings, etc.
        /// </summary>
        /// <remarks>
        /// Offsetting stringField not necessary if only creating a single line of buttons, because header adds a single line above the stringfield by nature. Beware - status line, if present, adds another one!
        /// </remarks>
        public class PopupData : IData
        {
            public PosString header;
            public PosString? statusLine;
            public StringField content;
            public List<Button> buttons;
            public int posx, posy, sizex, sizey;
            public string[] frame;
            string type = "popup";
            public string Type
            {
                get { return type; }
            }
            public PopupData(string header, string? statusLine = null, StringField? content = null, List<Button>? buttons = null)
            {
                this.content = content ?? new StringField(new string[0]);
                this.buttons = buttons ?? new();
                // Get the size of all inside elements
                int maxy, miny, maxx, minx;
                if(this.content.content.Count != 0)
                {
                    maxy = this.content.content[0].posy;
                    miny = this.content.content[0].posy;
                    maxx = this.content.content[0].posx + this.content.content[0].size;
                    minx = this.content.content[0].posx;
                } else if (this.buttons.Count != 0)
                {
                    maxy = this.buttons[0].posy;
                    miny = this.buttons[0].posy;
                    maxx = this.buttons[0].posx + this.buttons[0].size;
                    minx = this.buttons[0].posx;
                } else if (this.statusLine is not null)
                {
                    maxy = (PlannedHeight - 3) / 2;
                    miny = (PlannedHeight - 3) / 2 + 2;
                    minx = Math.Min((PlannedWidth - statusLine.Length) / 2, (PlannedWidth - header.Length) / 2);
                    maxx = Math.Max(minx + statusLine.Length, minx + header.Length);
                } else
                {
                    maxy = miny = (PlannedHeight - 1) / 2;
                    minx = (PlannedWidth - header.Length) / 2;
                    maxx = minx + header.Length;
                }
                foreach (PosString item in this.content.content)
                {
                    if (item.posy > maxy)
                    {
                        maxy = item.posy;
                    }
                    if (item.posy < miny)
                    {
                        miny = item.posy;
                    }
                    if (item.posx + item.size > maxx)
                    {
                        maxx = item.posx + item.size;
                    }
                    if (item.posx < minx)
                    {
                        minx = item.posx;
                    }
                }
                foreach (Button item in this.buttons)
                {
                    if (item.posy > maxy)
                    {
                        maxy = item.posy;
                    }
                    if (item.posy < miny)
                    {
                        miny = item.posy;
                    }
                    if (item.posx + item.size > maxx)
                    {
                        maxx = item.posx + item.size;
                    }
                    if (item.posx < minx)
                    {
                        minx = item.posx;
                    }
                }
                // Add statusLine - if present
                if (statusLine is not null)
                {
                    miny -= 2;
                    this.statusLine = new PosString(statusLine, (PlannedWidth - statusLine.Length) / 2, miny);
                    if(this.statusLine.posx < minx)
                    {
                        minx = this.statusLine.posx;
                    }
                    if(this.statusLine.posx + this.statusLine.size > maxx)
                    {
                        maxx = this.statusLine.posx + this.statusLine.size;
                    }
                } else
                {
                    this.statusLine = null;
                }
                // Add header
                if(this.content.content.Count != 0 || this.buttons.Count != 0 || this.statusLine is not null)
                {
                    miny -= 2;
                }
                this.header = new PosString(header, (PlannedWidth - header.Length) / 2, miny);
                if (this.header.posx < minx)
                {
                    minx = this.header.posx;
                }
                if (this.header.posx + this.header.size > maxx)
                {
                    maxx = this.header.posx + this.header.size;
                }

                //Create a frame
                posx = minx - 2;
                posy = miny - 2;
                sizex = maxx - minx + 4;
                sizey = maxy - miny + 5;
                frame = createFrame(sizex, sizey).Split("\n");
            }
            public void Write()
            {
                for (int i = 0; i < frame.Length; i++)
                {
                    CursorPosition(posx, posy + i);
                    ColorString(frame[i], Foreground, Background);
                }
                header.Write();
                if (statusLine is not null)
                {
                    statusLine.Write();
                }
                foreach(PosString item in content.content)
                {
                    item.Write();
                }
                foreach (Button item in buttons)
                {
                    item.Write();
                }
            }
            /// <summary>
            /// Clear says whether to overwrite text with background color or not
            /// </summary>
            public void Delete(bool clear = true)
            {
                if (clear)
                {
                    for (int i = 0; i < frame.Length; i++)
                    {
                        CursorPosition(posx, posy + i);
                        ColorString(frame[i], Background, Background);
                    }
                    foreach (Button item in buttons)
                    {
                        item.Delete();
                    }
                }
                foreach (Button x in buttons)
                {
                    x.Delete(clear);
                }
            }
        }
        /// <summary>
        /// Generic Write and Delete interface for data classes. Allows creation of new layouts without the need to chan the core.
        /// </summary>
        public interface IData
        {
            public string Type
            {
                get;
            }
            public void Write();
            public void Delete(bool clear);
        }
        #endregion
        #region Elements
        // Classes that define GUI elements
        // Positioned string - has a position, size, alignment, content and colors
        /// <summary>
        /// Creates a string with a position, size, alignment, content and colors. Default value of size is the length of the content, default value of color are global Foreground and Background.
        /// </summary>
        public class PosString
        {
            public int posx, posy, size;
            public string content, align, tag = "";
            public ConsoleColor? fore, back;
            public int Posx { get { return posx; } set {  posx = value; } }
            public int Posy { get { return posy; } set { posy = value; } }
            public string Content { get { return content; } set { content = value; } }
            public int Size { get { return size; } set { size = value; } }
            public string Align { get { return align; } set { align = value; } }
            public string Tag { get { return tag; } set { tag = value; } }
            public PosString(string content, int posx, int posy, int? size = null, ConsoleColor? fore = null, ConsoleColor? back = null, string align = "center")
            {
                this.posx = posx;
                this.posy = posy;
                this.content = content;
                this.size = size == null? content.Length : size.Value;
                this.align = align;
                this.fore = fore;
                this.back = back;
            }
            public virtual ConsoleColor Fore
            {
                get { return fore == null ? Foreground : fore.Value; }
                set { fore = value; }
            }
            public virtual ConsoleColor Back
            {
                get { return back == null ? Background : back.Value; }
                set { back = value; }
            }
            public virtual PosString Write(ConsoleColor? fg = null, ConsoleColor? bg = null)
            {
                CursorPosition(posx, posy);
                Console.ForegroundColor = fg == null ? Fore : fg.Value;
                Console.BackgroundColor = bg == null ? Back : bg.Value;
                Console.Write(StringFormat(content, size, align));
                Console.ForegroundColor = Foreground;
                Console.BackgroundColor = Background;
                return this;
            }
            public virtual PosString Delete(ConsoleColor? bg = null)
            {
                CursorPosition(posx, posy);
                Console.ForegroundColor = bg == null ? Background : bg.Value;
                Console.BackgroundColor = bg == null ? Background : bg.Value;
                Console.Write(new string(' ', size));
                Console.ForegroundColor = Foreground;
                Console.BackgroundColor = Background;
                return this;
            }
        }
        // Field of strings - as a block, for viewing
        /// <summary>
        /// Creates a block of strings. Default values of positions are centered, sizex is the longest string in the array, sizey is the total height of elements except header
        /// </summary>
        /// <remarks>
        /// Default values for align are center, for colors are global Foreground and Background. Can shift text upwards from center by setting posy to negative value. Same about posx to left;
        /// </remarks>
        public class StringField
        {
            int posx, posy, sizex, sizey;
            public List<PosString> content;
            public StringField(string[] inContent, int posx = 0, int posy = 0, int? sizex = null, int? sizey = null, string align = "center", ConsoleColor? fore = null, ConsoleColor? back = null)
            {
                if (inContent.Length == 0)
                {
                    // Empty Field - probably as a placeholder
                    this.posx = 0;
                    this.posy = 0;
                    this.sizex = 0;
                    this.sizey = 0;
                    content = new();
                    return;
                }
                if (sizex == null)
                {
                    int maxLen = 0;
                    foreach (string item in inContent)
                    {
                        if (item.Length > maxLen)
                        {
                            maxLen = item.Length;
                        }
                    }
                    this.sizex = maxLen;
                } else
                {
                    this.sizex = sizex.Value;
                }
                if (posx <1)
                {
                    this.posx = (PlannedWidth - this.sizex) / 2 + posx;
                } else
                {
                    this.posx = posx;
                }
                this.sizey = sizey == null ? inContent.Length : sizey.Value;
                if (posy == 0)
                {
                    this.posy = (PlannedHeight - this.sizey) / 2 + posy;
                } else
                {
                    this.posy = posy;
                }
                content = new();
                posy = this.posy;
                foreach (string item in inContent)
                {
                    content.Add(new PosString(item, this.posx, posy, this.sizex, fore, back, align));
                    posy++;
                    if (posy - this.posy > sizey)
                    {
                        break;
                    }
                }
            }
        }
        // Element - a middle step between button and a string. adds a press method
        public abstract class Element : PosString
        {
            public Element(string content, int posx, int posy, int? size, ConsoleColor? fore = null, ConsoleColor? back = null, string align = "center") : base(content, posx, posy, size, fore, back, align)
            {
            }
            public abstract void Press();
        }
        // Button - has an bAction, can be highlighted, pressed
        public class Button : Element
        {
            public Action<Button> bAction;
            ConsoleColor? hfore, hback;
            public int margin;
            public Button(string content, int posx, int posy, int? size, int margin = 0, Action<Button>? bAction = null, ConsoleColor? fore = null, ConsoleColor? back = null, ConsoleColor? hfore = null, ConsoleColor? hback = null, string align = "center") : base(content, posx, posy, size, fore, back, align)
            {
                this.bAction = bAction ?? new Action<Button>((x) => { });
                this.margin = margin;
                this.size = size == null ? content.Length : size.Value;
                this.posx = posx == 0 ? (PlannedWidth - this.size - margin)/2 : posx;
            }
            public override ConsoleColor Fore
            {
                get { return fore == null ? ButtonFore : fore.Value; }
                set { fore = value; }
            }
            public override ConsoleColor Back
            {
                get { return back == null ? ButtonBack : back.Value; }
                set { back = value; }
            }
            public ConsoleColor HFore
            {
                get { return hfore == null ? HighFore : hfore.Value; }
                set { hfore = value; }
            }
            public ConsoleColor HBack
            {
                get { return hback == null ? HighBack : hback.Value; }
                set { hback = value; }
            }
            override public void Press()
            {
                bAction(this);
            }
            public override Button Write(ConsoleColor? fg = null, ConsoleColor? bg = null)
            {
                if(content == "")
                {
                    return this;
                }
                CursorPosition(posx, posy);
                Console.ForegroundColor = fg == null ? Fore : fg.Value;
                Console.BackgroundColor = bg == null ? Back : bg.Value;
                Console.Write(StringFormat(StringFormat(content, size, align), size+margin));
                Console.ForegroundColor = Foreground;
                Console.BackgroundColor = Background;
                //enter data into ButtonField
                for (int i = posx; i < posx + size + margin; i++)
                {
                    ButtonField[posy, i] = this;
                }
                return this;
            }
            /// <summary>
            /// By default, Writes over the button with a color. If first parameter set to false, only removes button from buttonField and refreshes it to not be highlighted.
            /// </summary>
            public Button Delete(bool clear = true, ConsoleColor? bg = null)
            {
                if (clear)
                {
                    CursorPosition(posx, posy);
                    Console.ForegroundColor = bg == null ? Background : bg.Value;
                    Console.BackgroundColor = bg == null ? Background : bg.Value;
                    Console.Write(StringFormat(StringFormat(content, size, align), size + margin));
                    Console.ForegroundColor = Foreground;
                    Console.BackgroundColor = Background;
                } else
                {
                    CursorPosition(posx, posy);
                    Console.ForegroundColor = Fore;
                    Console.BackgroundColor = Back;
                    Console.Write(StringFormat(StringFormat(content, size, align), size + margin));
                    Console.ForegroundColor = Foreground;
                    Console.BackgroundColor = Background;
                }
                //remove data from ButtonField
                for (int i = posx; i < posx + size + margin; i++)
                {
                    ButtonField[posy, i] = null;
                }
                return this;
            }
            public void Highlight(ConsoleColor? fore = null, ConsoleColor? back = null)
            {
                if (content == "")
                {
                    return;
                }
                fore = fore == null ? HFore : fore.Value;
                back = back == null ? HBack : back.Value;
                Write(fore, back);
            }
            public string GetInput(bool insertIntoButton = false, int? maxLen = null, int? priority = null, bool showFrame = false, bool number = false, bool empty = true)
            {
                Delete();
                int max = maxLen ?? size;
                int prior = priority ?? PopUpPriority+1;
                int ypos = showFrame ? posy-1 : posy;
                int xpos = showFrame ? posx - 2 : posx;
                StringBox box = new(max, "", xpos, ypos, size + margin + 4, size + margin, prior, showFrame);
                string output = box.Get(ref keyTunnel, content, number, null, empty);
                if (insertIntoButton)
                {
                    content = output;
                }
                Highlight();
                return output;
            }
        }
        // Makes whole lines of buttons with certain spacing, sizing and/or margins
        public static List<Button> CreateButtons(int posx, int posy, int size, int buttonMargin, string[] inButtons, Action<Button>?[] bActions, ConsoleColor? buttonFore = null, ConsoleColor? buttonBack = null)
        {
            int buttonSize;
            Button[] res = new Button[0];
            #region ButtonLine button sizing
            while (bActions.Length < inButtons.Length)
            {
                bActions = bActions.Append(null).ToArray();
            }
            if (buttonMargin < -1)
            {
                //buttons the size specified
                buttonSize = -1 * buttonMargin;
                int spacing = (size - buttonSize * inButtons.Length) / (Math.Max(inButtons.Length - 1, 1));
                if (posx == 0)
                {
                    posx = (PlannedWidth - buttonSize * inButtons.Length - spacing * (inButtons.Length - 1))/2;
                }
                for (int i = 0; i < inButtons.Length; i++)
                {
                    res = res.Append(new Button(inButtons[i], posx, posy, buttonSize, 0, bActions[i], buttonFore, buttonBack)).ToArray();
                    posx += buttonSize + spacing;
                }
            }
            else if (buttonMargin == -1)
            {
                //equally divided buttons - spacing = 1
                buttonSize = (size - inButtons.Length - 1) / inButtons.Length;
                if (posx == 0)
                {
                    posx = (PlannedWidth - buttonSize * inButtons.Length - (1 + inButtons.Length)) / 2;
                }
                for (int i = 0; i < inButtons.Length; i++)
                {
                    res = res.Append(new Button(inButtons[i], posx, posy, buttonSize, 0, bActions[i], buttonFore, buttonBack)).ToArray();
                    posx += buttonSize + 1;
                }
            }
            else
            {
                //all buttons their content length + margin
                buttonSize = 1;
                foreach (string button in inButtons)
                {
                    buttonSize += button.Length + 2 * buttonMargin;
                }
                if (posx == 0)
                {
                    posx = (PlannedWidth - buttonSize) / 2;
                }
                if (buttonSize > size)
                {
                    Exception e = new Exception("Buttons too long");
                    throw e;
                }
                for (int i = 0; i < inButtons.Length; i++)
                {
                    res = res.Append(new Button(inButtons[i], posx - buttonSize, posy, inButtons[i].Length, buttonMargin, bActions[i], buttonFore, buttonBack)).ToArray();
                    buttonSize -= inButtons[i].Length + 2 * buttonMargin + 1;
                }
            }
            return res.ToList();
            #endregion
        }
        // WIP
        // String box for entering data into the program
        public class StringBox
        {
            protected int posx, posy, maxLen, frameSize, fieldSize, priority;
            protected string header;
            protected bool showFrame;
            ConsoleColor? fore, back;
            public ConsoleColor Fore
            {
                get
                {
                    return fore is null ? Foreground : fore.Value;
                }
                set
                {
                    fore = value;
                }
            }
            public ConsoleColor Back
            {
                get
                {
                    return back is null ? Background : back.Value;
                }
                set
                {
                    back = value;
                }
            }
            public StringBox(int maxLen, string header, int posx, int posy, int frameSize, int fieldSize, int priority, bool showFrame, ConsoleColor? fore = null, ConsoleColor? back = null)
            {
                this.posx = posx;
                this.posy = posy;
                this.maxLen = maxLen;
                this.header = header;
                this.frameSize = frameSize == 0 ? PlannedWidth - posx : frameSize;
                this.fore = fore;
                this.back = back;
                
                if (fieldSize == 0 & showFrame)
                {
                    this.fieldSize = this.frameSize - (4 + 3 * Math.Min(header.Length, 1) + header.Length);
                } else
                {
                    this.fieldSize = fieldSize;
                }
                this.priority = priority;
                this.showFrame = showFrame;
            }
            public virtual string Get(ref Channel<ConsoleKeyInfo> keyTunnel, string content = "", bool number = false, Debugger? debugger = null, bool empty = true, bool clearFrame = true)
            {
                PosString input;
                string result = content == " " ? "": content;
                // Position - cursor in the field
                // Index - first printed character from the string
                int position = Math.Min(fieldSize, result.Length), index = Math.Max(0, result.Length - fieldSize);
                if (showFrame)
                {
                    input = new PosString(result, posx + 2 + 3 * Math.Min(header.Length, 1) + header.Length, posy + 1, fieldSize, null, null, "left");
                    InputFrame(header, frameSize, posx, posy);
                } else
                {
                    input = new PosString(result, posx + header.Length + Math.Min(header.Length, 1), posy, fieldSize);
                }
                ConsoleKeyInfo pressedKey = new ConsoleKeyInfo();
                TextIn = true;
                Console.CursorVisible = true;
                keyTunnel.Writer.WriteAsync(new ConsoleKeyInfo(((char)ConsoleKey.Tab), ConsoleKey.Tab, false, false, false));
                input.Write();
                while (pressedKey.Key != ConsoleKey.Enter || (input.content.Trim() == "" && !empty))
                {
                    // Get the input
                    pressedKey = KeyInput(priority, input.posx + position, input.posy, false);
                    // Work the input
                    if (pressedKey.Key == ConsoleKey.Escape)
                    {
                        input.Write(Background, Background);
                        Console.CursorVisible = false;
                        TextIn = false;
                        if (showFrame && clearFrame)
                        {
                            InputFrame(header, frameSize, posx, posy, true);
                        }
                        if (content == "") content = " ";
                        return content;
                    }
                    if (pressedKey.Key == ConsoleKey.Backspace)
                    {
                        try

                        {
                            result = result.Remove(index + position - 1, 1);
                            if(index > 0)
                            {
                                index--;
                            }
                            else if (position > 0)
                            {
                                position--;
                            }
                        }
                        catch (Exception e)
                        {
                            // presumed empty, do nothing
                        }
                    }
                    else if (pressedKey.Key == ConsoleKey.Delete)
                    {
                        try

                        {
                            result = result.Remove(index + position, 1);
                        }
                        catch (Exception e)
                        {
                            // presumed nothing in front
                        }
                    }
                    else if (pressedKey.Key == ConsoleKey.LeftArrow)
                    {
                        if(index > 0)
                        {
                            if (position > fieldSize / 2)
                            {
                                position--;
                            }
                            else
                            {
                                index--;
                            }
                        } else if (index == 0 && position > 0)
                        {
                            position--;
                        }
                    }
                    else if (pressedKey.Key == ConsoleKey.RightArrow)
                    {
                        if(result.Length > fieldSize)
                        {
                            if (index < result.Length - fieldSize)
                            {
                                if (position < fieldSize/2)
                                {
                                    position++;
                                }
                                else
                                {
                                    index++;
                                }
                            } else if (index == result.Length - fieldSize && position < fieldSize)
                            {
                                position++;
                            }
                        } else if (position < result.Length)
                        {
                            position++;
                        }
                    }
                    else if (result.Length < maxLen && (result.Length > 0 || pressedKey.Key != ConsoleKey.Spacebar) && !Char.IsControl(pressedKey.KeyChar) && (!number || Char.IsNumber(pressedKey.KeyChar)))
                    {
                        if(index + position == result.Length)
                        {
                            result += pressedKey.KeyChar;
                        } else
                        {
                            result = result.Insert(index + position, pressedKey.KeyChar.ToString());
                        }
                        if(index + position < fieldSize)
                        {
                            position++;
                        } else
                        {
                            index++;
                        }
                    }
                    // Display the input
                    input.content = StringFormat(result.Substring(index, Math.Min(fieldSize, result.Length - index)), fieldSize, "left");
                    if (result.Length == maxLen)
                    {
                        input.Write(ConsoleColor.Black, ConsoleColor.DarkGray);
                    }
                    else
                    {
                        input.Write(Fore, Back);
                    }
                }
                input.Write(Background, Background);
                Console.CursorVisible = false;
                TextIn = false;
                if (showFrame && clearFrame)
                {
                    InputFrame(header, frameSize, posx, posy, true);
                }
                if(result == "") result = " ";
                return result;
            }
        }
        #endregion

        #region Display module
        // Create an empty frame around the screen - cleans the rest of the screen
        public static string createFrame(int width = 0, int height = 0)
        {
            width = width == 0 ? PlannedWidth : width;
            height = height == 0 ? PlannedHeight : height;
            string frame = "+" + new string('-', width - 2) + "+\n";
            for (int i = 0; i < height - 2; i++)
            {
                frame += "|" + new string(' ', width - 2) + "|\n";
            }
            frame += "+" + new string('-', width - 2) + "+";
            return frame;
        }
        // WIP
        // Protected methods for error handling 
        public static void CursorPosition(int x, int y)
        {
            try
            {
                Console.SetCursorPosition(x, y);
            }
            catch (Exception)
            {
                Console.SetCursorPosition(1, 1);
            }
        }

        #region frames
        // Easter egg. Not an official method
        public static void Disappear()
        {
            CleanFrame();
            string[] strings = new string[] {
"X$XXxXXXX$$$$$;++++++$;x+++++++XXX+;++++++;+++++++++;++++++++++++++;+;;;;;++++++++++;;;;;;;+++;;;;;;:+++:;;;;;;+++++++++++;;;::::++++++++XX$$$$X:::::::X+::::::X$XX++::::::xxxXX$$$$$$$$Xx++;.......;++++++++++",
"$$XXXXXX$$$$$$;+++++x$+xxxx++XXX+++;++++++;xx++++x+++++++++;++++++++++++;;++++++++++;++;;;;+++;;;;;;;+++;;;;;:;+++++++++++;;;::::+++++++++xXX$$$:::::::XX::::::+$$Xx+::::::XXXX$$$$$$$XXxx+++.:.....+++++++++++",
"XXXXXX$$$$$$$X;++++xX$X++x+x+++++++;+++++x+;+x+x+x+++++++++;++++++++++++;;++++++++++;+++;;;+++;+;;;;;+++;;;;;;:+++++++++++;;;;;::++++++++++xXXX$:::::::+X::::::;$$XX+::::::XXX$$$$$$X$$$Xx+++::.....+++++++++++",
"XXXXX$X$$&$$$X;+++++++;+x+xxx++++++;++x+xxX;x++xxxx++XxXXXx++++xx+++;+++++++++++++++;+++++;+++;++;;;;+++;+;;;;;+++++++++++;;;;;::+++++++++++xXXX:;:::::++:::::::$$$X+::::::X$$X$$$$$&$XX+++++::::..:+++++++++++",
"XXXXX$$$$$$$$+;++++++++++xxxxx+++++;+x+xxxXxxxxxx+xxxX$XXXX++++xx+++x+x+++++++++++++;+++++++++;+++++++++;++;;;;+++++++xxXX;;;;;::x+++++++++++xXX:;::::;++;::::::$$$X+::::::$$$$$$$$$$$$$$$Xx+:::.:.:+xxx+xxx+++",
"XXXXX$$$$$$$X;;++++++++++x++xxxxxxX;++++xx$X;+xxxxxxxXXXXX++xx+x+xxxx++xx+++++++++++;+++++++++++xxXxXxxxx++xx+;+++++xXXXx+;+;;;;:&$Xx+++++++++xx:;;;::;X++::::::X$$$+::::::$$$$$$$$$$$$$$$$$X::::..:XXXXXXXXXXX",
"XXX$$$$$$$XXX;;++++++++++++x+xXXXXx;++++xxXX;+x+xxxxxX$$$X;x+xxxxxxxxx+xxx++++++++++;+xx++++XXXXXXXXXXXxx+++xxxXXx++++++++;+;;;;;+xX$XXxx++++++x:;;;;:;Xx+:;;:::+$$$+::::::$$$$$$$$$$&&$$XXXX:::::.:XXXXX$XXXX$",
"X$X$$&$$$$$$X;;;+++XXXXX;+++xxXX$$x;+++++xX$$;+++xxxxXXXXX+++xxxx+++++xxxxx++++++++++XXXX$$$$$$$$$$XXXXxx+++xxxXXXXxx+++++;++;;;;++++X$$$XX+++++;;;;:;;$X+:;;;:::$$$+::::::&$$XX$$&&$$X+++++X:::::::$$XXXX$$$$$",
"XXX$$$$$$$$$+;;;+++XXXX$:;++++xXXXX;;+++xxXX$;;++++xXXXXXx++xxxx+++++;+xxxx++++++xXXXXXXX$$X$$$$$$$$XXXXxx+xXxXXxXXXXx++++;++;;;;++++x+xX$$$XX$$;;;;;;;$Xx;;;;;::$$X+;:::::$XXX$$$$$XXx++xXXX:::::::$$$$$$$$$$$",
"+;;;;;;;XXXX;;+;+++X$$$$;;;++++XX$$;;++++X$$$+;++++xx++++++++xxX+++++;++xxxx++++XXXXXXXxX$XXXX$$$$$$$$$$X+++xXXXXXxXXXXx++;+++;;;++++++++xX$$$$$;;;;;;;XX$+;;;;;:X$X+;;;:::$$XX$$$$Xx+xxXXXXX:::::::$$$$$$$$XXX",
"++++++++X$$$;;;;;++$$$$$+;;;+++$$X$;;++++x$$XX;;++++x++++;++++xx++++++++xxxx++xXXXX$XXXxXXXXXXXX$$$$$$$X++xXXXXXXXXXXXXXX+;++++++;;;;;;;;xX$$&&&;+;;;;;$$$X;;;;;;X$$+;;;;::::::::::XXXXX$XXX$::::::;$$$$$$$$XXX",
";;;;;;;+X$$$;;;;++X$$$$$X;;++++X$$$:;;++++XXXX:;+++++Xx++;++xxx+++++++;+xxxx+XXXXXXXXXx+xxxXxxxxxxXXX$$$$$$+X$X$X$XXXXXXXX;++++++++;+;;;;XX$$$&$;+;;;;;$$$$;;;;;;+$X+;;;;;;;:::::::$$$$$$$$$$::::::;$$$XXXXXxx+",
";;;;;;;;x$$x;;;;;;$$$$$$$:;;;;;+XXX:;;;;;x$&$$X;;;+++XXXx;;+++++++++++;++++xXXXX$$$XXXXXXXxx++++;;;;;++XXX$$$$$$$$$XXXXXXXX+++++++++++;;;XX$$X$X;+;+;;+X$$$:;;;;;;$X++;;;;;;;;;;:::$&&$&$$$$$::::::;$$$$Xxx++++",
"$$$$&$$$$$X$$$$$$$$$$$$$$$$$$X$XX$$$$$$$$$$$XXXXXXXXXXXxxx+++++++++++++++++XXXX$$$$$XX$Xxx++;;;;;;;;;;;;+xxxX$$$$$$$XXXXX$XX+++xxXXx+++xX$XXXXX$$$$$$$$$X$$$$$X$$$$XxxxX$$$$$$$$$$$&$$X$$$$$$$$$$$$$$$XXXx+++++",
"$$$$$$$$$$$$$$$$$$$$$$$$$XX$X$$$$XXX$$$X$XXxXx++++++++++++++++++++++++++++XXXX$$$$$$$$Xx++;;;;;;;;;;;;;;;;+xXXX$$$$$$XXXXX$XX++xxXX$$XxxxxX$$$$$$$$$$$$$$$$$$$X$$$XXxxX$X$$$$$$$&$$$$$$$X$XXXXX$$$$XXxxxxxxx++x",
"$$&$$$$$XX$$$$$$$$$$XXXX$$$$$$$$$$XX+++++++++x+++++++++++++++++++++++++++X$X$$$&$$$$$XXx++;;;;;;;:::;;;;;;;;+xXx$$$$$$XXXXX$X++++++x$$$$x+X$$$$$$XXXXXXXXXXXXXXX$$XXXXX$$$$$$$$$$$$$&&&$$$$XXXXXXXX$XXXxXXxxxXX",
"$$$$$$$$$$$$$$$$$$$$$X$$$$$$XXXxx+++++++++++xxx+++++++++++++++++++++++++XX$X$&$$$$$$XXx+++;;;;;;;;;;;;;;;;;+;++XX$$$$$XXX$XXXX+++++x++X$&XXX$$$XXXXXxxX$$$XXXXX$$$XXXXXXX$$$$$$&$$$$$$$$X$XXXXXXXXXXXXXXXXxXXXX",
"$$$$$$$$$$$$$$$XX$$$$$XXXXXxXXX++++++++++++++++++++++++++++++++++++++++XXXX$&$$$$$$$$Xx++;;;;;;;;;;;;;;;;+++++++XX$$$$$XX$$XX$xxxxxxXXXX$$&$$XX$$$$$XX$$$$$$X$$$$$XXXXXXXXX$X$$$$$$$$$XXXXXXXXXXXXXXXXX$$XXXXX$",
"$$$$$$$$$$$$$$$$$$$$$$$XXXXxXXxxXxx+x+++++++++++++++++;+;+++++++++++++xXXX$$&$&&&&$$XX++++;;;;;;;;;;;;;;;++++++++X$$$$$X$X$$X$XxxxxXXXXXX$$$$$$$$$$$$$$$$$$$$$$$$$$X$XXXXxXX$$$$$$$$$$$$$XXXXX$$$XXX+XXXXXXXXXX",
"$$$$$$$$$$$$$$$$$$$$$$$X$$X$$$$$$XXXXXXXXXXx++++x+++++++++++++++++++++XXXX$&&$&$&&$$Xx+++;;+;;;;;;;;;;;;;++++++++xX$$$$$$X$$$X$XxXXxXXXX$$$$$$$$$$$$$$$$&$&$&$$$$XXXXXXXXXXXXXXX$$$$X$$X$$$$$$$$$$$XXXXXXXXXXXX",
"$$$$$$$$$$$$$$$$$$$$$$$$$X$$$$$$$$$$$$$$$$XXXxxx+xxxxxxxxxxx+++++++++XXXXX$&$&&&&&$$x+++;;;;+;++;;;;;;;;;+++++++++XX$$$$X$$$$X$XX$$$XXXXXX$$$$$$$$$$$$$$$$$$$$&$&$XXXxxXXXXXXXX$$$$$$$$$$$$&&$&$$$$XXXXXXXXXXXX",
"$$$$$$$$$$$$&$$$$&&$$$$$$$$$$$$$$$$$$$$$$$&$$$XXx++++++++++++++++++++XXxX$$&&&&$&&$++++++++xxxx+++++++++++++++++xxX$$$$$$$$$$X$$X$$$XXXXXXXXXX$$$$$$$&&&&&&$$$$$$$XXXx++xXXxXXX$$$$$&&&$$&$$&$&&$$$$$&&$$&&&&&$",
"$$$$&$$$$$$$$$$$&&&&&&&$&$$$$$$$XXXXX$$$$$$$$&$$XX++++++++;;;;;;;;+;XXxxX$$&&&&&&$+++xxxxxxXX$$$$$XX+++++++XXX$$$$$$$$$$X$$$$$$$$XXXXXXXXXXXX$$&&$$&&$$$$$$$$&$$$$$$$$$$XXxxxX$$$$&&&&&$$$$&$$$$$$$X$$X$XXXXX$$",
"$$$&&&$$$$$XXXXXX$$$&&&$$&$$XX$$$$$XX$$$XXXXXX$$$$XXXx+++++++;;+;+++XXxX$$&&&&$&$x++++XXX$$$$$&$$$$$X+++++$$$$$$$$$$&$$$$$$$$$$$XXXXXXXXXX$$$$$$$$$$$$$$$$$$$$&&&$$$$$&$$XXXX$$$$$$$X$$&$$$&&&$$$X$$$$$$$$$$$XX",
"$$$$$$$$$$XXX$X$XXXX$$&&$$$$$X+xXXXX$$$$$$$$$$$$$$$XXx+xx++++++++++XXXxX$X$&&&$&$+++++x$$$$&&&$$$$$$x+;;+x$$&&&&$$$&&$$$$$$$$$$$$XXXXXXXX$$X$$$$$XX$$$$$$$$$$$$$$$$$$XXXXXxxx$&&$$X$$$$$$$$&&$$$$$$$$$$$$$$$$XX",
"$$&&&&$$$XXX$$$XXX$$&&&$$$$$$$Xx+xx+++xxXX$$$$$$$XX$$XXxxxx++++++++XXXXXXX$&&&$&X+;;;+++x$XX$$Xx$$$X++;;+X$$$&$&&$&$&$$$$$$$$$$$$XxXXXXX$$XXX$$$$$$$$$$$$$$$$$$$$$XXXX$$$X+xX$&&&&$$$$$$$$$$$$$$$$$$XXX$X$$$$$$",
"&&$$&&$$$$X$&$XXxx+xX$&&&$$$$$$XXx+xX+++++++xX$$$$$$XXX$XXx+++++++XXXxxXXX$&&&$$x+;;;;;;+xXXXXx+++++++;;+X$xxxx++X$&&$$$$$$$$$$$XXXXXXX$$$$$$XX$$$$$$$$$$$$$$$$$$$$$$$$$&&XX$$&&&$$$XXXXXXX$$$$$$$X$Xxxxx+xxXX$",
"&&$$&&&&$$$$$$$$XxxXXX$$$$$$$$$$XXXXXXXx++++++xXXX$$$XXXXxxx++++++XXXxXxXxX$&$$XX++;;;;;;;;;;;;;;;;+++;;+xX++++xXX$$&X$$$$$$$$$$$XXXXXXX$X$X$$$$$$$$$$$$$$$$$$$$$$$$$$$$$&&$$$&&$$$XXXXXXXXX$$$$$$$$Xxxx+++xxx$",
"&&$$$$&$$$$&&&&&$Xx+xXX$XX$&$$$$$$$$$$XXXx++++++xXX$XXXX$$XXxx+++XxXXXXxXxxX$$X+Xx++;;;;;;;;;;;;;;;+++;;;xX++++++++x$XX$$$$&$$$$$X$XXXXXXX$$$$$$$$$$$$$$$$$$&&$$XX$$$$$$$$$&$&&$$$$$$$$XXXXXXXXX$$$$$XXX+++xx+x",
"$$&$$&$$$$X$$$&&&&Xx+++xxX$$&$$$$$$$XXXXXXx++++++++x$$$XXXXXXxxxxXXXXxXXxXxxX$$xXX+++++;;;;;;;;;;;++++;;;+XX+++++++++$$$$$$&&$$$X$X$X$XXXX$$$$&$$$XX$$$$&&$$X$$$$$&$$XX$$$&&$&&$$$XXX$$$$XXXXXXXXX$&&$$XXxx++xx",
"&&&$$$$$$$XX$$$$$&$Xx+;;++X$&&&$$$$$$$$$$XXXxx+++++++$$$$XXxxxxxXxXXXxXXXXXxxXXXxXx++++++;;;;;;;+++++x+++xXx+++++++xX$$$$$$$&$$XX$XXXXXxx$$$$&$$$XXX$$$$$$$$$$$$$XXX$$$$&$$$&&$$$$XX$$X$$$XXXXXXXXXX$$$$$XXXx+x",
"$$$$$&$$$$$XX$$XX&$XX+++++X$&&&$$$$$$$$$&$$XXxx++++++xX$$$$XXXXxXXXXXxX$$$x$XXXXxxXxx++++++;;;;;++xX&&X$$$$$++++++xxX$$$$$$$&$$XX$$XXXXxxX$$$$$$$$$&$$$$$$$&$$&$XXX$X$$$$$$$$$$$$$$XX$X$$$$$XXXXXXXXX$$$$$$XXxx",
"$$$$$$$$$$$XX$$$XX$$$X++++X$$&$&&&&$$$$$$&$$$Xxx+++++xXXX$X$$XXXXXXXXXXX$$XX$XXXXxxxx++++++++;;;;;X$$$$$$$$X++++++xX$$$$$$&$$$$XX$$XXXxXXX$&$$$$$$$$$$$$$$$$&$$$XXXxX$$$$$$&$$$$$$$$$$$XX$$XXXXxxXXXXXX$$$$XXXX",
"&$$$$$$$XXXXXXX$XXX$&&X++++XX$$&&&&&&&$$$$$$$$XXXxxxXXXXXXXX$$$XXXXXXXXX$$$$X$$XXxxxxx++++++;+;;;;;;++XXXXXX+++++xXX$$$$$$&&$$$$X$$XXxxxX$$$$$$$$$$$$X$$$$$$&&$$$xx$$XX$$$$$$$$$$$$$$$$$XX$XXXXxXXXXXXXX$$$XXXX",
"&$$$$$$$XXXXxXX$$XxXX$$x+++X$$$$$&&$&&&$$$$$$$$$$$XXXXXXXXXXX$$X$XXXXXXX$$$$$$$$XXx+xx+++++++;;;;;;;;;+++xxx+++xxXX$$$$&$&$&$$$$X$$XXxXXX$X+X$&$$$$XXX$$$$$$$$$$$$XXX$$$$$$$XXXXX$$$$X$$XX$$XXXXXXXXXXXXXX$$$XX",
"&$$$$$$$$$$XXXXX$XXXX$$Xx++x$$XX$$$$&&&&&&$$$$$$$$$X$$XXXXXXXXX$XX$X$XXX$X$$$$$$$XXxxx+++++++++++++XXXXXXXx+++xxxX$$&$$$$&$$$$$$X$$$XXXXXXxX$$$$$XXXXX$$$$$$$X$$$XX$XXX$$$$$xxxXxX$$$$$$$X$$$$$XXXXXXXXXXXX$$$$",
"$$$$&$$X$$$X$$$$$$XXXXX$X++x$$XXXXX$$$$$$$&$$$$$$$$$$$XXXXXXXXX$$$XX$XXX$X$$$$$$$$XXxx+++++++xXX$$$$$$$$$$$$$$XXXX$$$$$$$$&&$$$$$$$$XxxXXX$$$$$$XXXXXX$$$$$$$$$$$XXXX$$$$$$XXXXXXX$$$$$$$XX$$$$$XXXXXXXXXXX$$$$",
"$$$$$XXxx$$$XX$$$$XXXXX$Xx+x$$$$XXXXX$$$$$&$$$$$$$$$$XXXXX$$XXXX$$$$$XXX$$$$$$$$$$$$Xxx++++++++++xx++++++xxxXXXXX$X$$$$$$$&&$$$$$$$$XxXXX$$$$$XXXXX$$$$$$$$$$$$$$$X$$$$$$$XXXxXXXXX$$$$$$XX$$$$$XXxXXXXXXXXX$$$",
"$$$$$$XX$X$$XXXX$$XX$$X$XXXX$$$XXXXXXXX$$$$$$$$$$$$$$$$XXXXXXXX$$$$$$XXX$$$$$$$$$$$$$XXxx+++++++xXX$$X$XXXXXXXXXXX$$$$&$$$&$$$$$$$$X$xXXXX$$$XXXXX$$$$$$$$$$$$XX$$$XX$$$$XXXXXxXXXX$$$$$$XXX$$$XXXXXXXXXXXXXX$$",
"$$&&&&$$$$$$X$XXXXXX$$$$$XXX$$$$XXXXXXXX$$$$$$$$$$$$$$$$$$$$$X$$$$$$$$XX$$$$$$$&$&$$$&$XXxx++++++++++xXXXXxxxXXxX$$$$$$$$$&$$$$$$$$X$X$XX$$$XXXXXX$$$X$XX$&&$$$X$$XX$$$$$XxXXxxXXXXX$$$$XXXX$$$XXXXXXXXxXXXXX$$",
"$$&&&&$&&&$XX$$XX$XX$$$$$XXX$&$$XXXX$$$$$$XX$$$$$$$$$XX$$$XXX$$$$$$$$$$$$$$$$$$$&$&$$&&$$XXx+++++++;++++++++XXxX$$$$$$$$$$&$$$$$$$$X$X$$$$XXXXXXXX$$X$$$$$$$$$XXXX$X$$$$$$XXXXX$$XXX$$$XXXXX$$$XXXXXXXXXxXXXXXX",
"$&$$$$$&$$&$&&$XX$$XX$$$$XX$$&$$X$XXXXX$$XX$X$$$$$$$XXXXXXXX$$$$$$$$$$$$$$$$&$$&$&&&$$&&&$$$XXx++++++++++xxXXxX&$$$$$$$$$&&&&$$$$$$$$$$$$XXXXXXXX$$$$$&&$$$$$$$XXXX$X$$$$$XXXXxXX$$XX$XX$$XX$XX$$$X$$$$$$XXXXXX",
"$&&&$&$X$$$$&$$$XXX$$$&&&X$$&&&$$$$XXXXXXXXX$$X$$$$$XXXXXXX$$$$$$$$$$$$$$$$$&&$$&&&$&$$$&&$$$$$$XXXXX$XX$X$XXX$$$$$$$$$$$$&&$&$$$$$$$$X$XXXXX$$$$$$&&&&&&&$&&&$$$XX$$$$$$$$$$$$XXxX$X$$$$XXX$$$$$$$$$$$$$XXX$X$",
"&&&&&$$$XXX$$$$$X$X$$$&&$X$&&&&&$$$$X$XXXXXXX$X$X$$$$X$$XxX$$$$$$$&$$$$$$$$$&&$$$$&&$$$&$&$$$$$$$$$$$$$$$&XXXX&$$$$$$$$$$&&&$&$$$$$$$X$XXXXXX$$$$$&&$$$&&$&&&&&$XXXX$$$$$$$$$$$$$XXXXXX$$$XX$$$$$$&&&$$$$$$$XX$",
"&&&&$$X$$$$$$X$XXXXX$&&&$X$&&&&&$$$$$$$$$$$X$XX$$XX$$$$XX$$$$$$$$$&&$$$$$$$$$&$$$&$$$$$$&$$$&$$$$$$$$$$&&$XxX$$$$$$$$&$$$&&&&$&$$$$$$$$XX$$X$$$$$$&$$$$$$XX$$&&$XXXXX$XX$&$$$$$$$XX$$XX$$$$XXX$$$&$XXXXXXX$$$$X",
"&&&$$$$$$$$$$$$$XXX$$&&&$X$$&&&&&&$$$$$$$X$$X$$$$$XX$X$$$$$$X$$$$$$$$$$$$$$$$&&$$$$$$$$$$$$$$$$$$$$$$&&&$XXX$&$$$$$$&&$$$&&&$$&$$$$$$$$$$XXXX$$$&&&&$$$$&&$$&&&$XXXX$$XX$$$XXXXX$XX$$$$$$$XXXX$$$$$$XXXXXX$XX$$",
"&&&&&$$&$$XXX$$$XxX$$$$&$X$&&&$&$$$$X$$$$$$$$$XXXXXX$$$X$$$$$$$$$&$$$&$$$$$$$$$$$$$$$$XX$$$$$$$$$$$$$$&&$XX$$$$$$$$&&&&$$$&$&$$&$$$$$$X$X$$$$$$$$&$&$&&&&&$$&&&$XXXX$XXX$$XXXX$X$$$$$&$$$$$X$XXXXXXXXXXXxxXXX$$",
"&$$$$$$$$$XXX$$$$X$$$$$&&$$$&&$$$$$$$$$$XX$$X$XXXXX$$$X$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$X++$$$$$$$$$$$$$&&XX$$$$$$$$$$&&&$$&$$$$&$$$$$$$$X$$$&$$$$$$&&&&&$&&$$$&$$$XXXXxxXXXXX$$$$$$&&$$&$$$XXXXXXXXXXXXXXXXXX$$X",
"&$$XX$XX$$$$$$$$$$$$$$$&&&$$$$$$$$$$$$$$$$$$$$$XXXX$$X$$$$$X$$$$$$$$&$$$$$$$$$$$$$$$$$X+++X$$$$$$$$$$$&&X$$$$$$$$$$$&&&&$$$$$$$&$$$$$$$$$XXXX$$$&&&&&&&&&$$$$$$$$$X$XXXXXXXXXXXXX$$$$&&&$$XXXX$$$$$XXXXXXX$$XXX",
"&&$$$$XX$$$$$$$&&$$$$&&&&&&&&$$$$$$$$$$$$$$$$$&$XXXXX$$$$$$$$$$$$$$&$$$$$$$$$$$$$$$$$$$+++++$$$$$$$$$$$&$$$$$$$$$$$$&$$&&$$$$$$&$$$$$$$$$XXXX$$$$&&&$&$&&&$$&&&&&&&$$&$XX$$$$XXX$$$$&&&&&$$$X$$&$$$$$$XXX$X$$$$",
"&&$XXX$X$$$&&&&&&&$$&&&&&&&&&&$$$$$$$$$$$$$$$$&$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$XX$$x+++++X$$$$$$$$&$$$$$$$$$$$$$$$$$&$$$$$$$$$$$&$$$X$XX$$&&&&&&&&&&&&&&&&&&&&&&&&$XX$&$XXXXX$$$&$&&&$$$$$$$$$$$$$$$$$$$$$$",
"&&$$$$$X$$&&&&&$&&&$&&&&$&&$$&&&&&$$$$$$$$$$$$$&&$$$$XX$$$X$$$$$$$$$$$$$$&$$$$$$$$$$X$Xx+++++++X$$$$$$$$$$$$$$$&$$$$$$$$&$$$$$$$$$$$$$$$$X$$$$&$&$$&&&&&&&&&&&&&&&&&&&&$X$&$$$$$$&&$$$$$$$$$$$XX$$$$$$$$$$$$$$$",
"&&&&$$$$$$&&&&$$$&$$$$$$$$$$$$&&&&&&$$&$$$$$$&&&$XX$XX$$$$XX$$$$$$$$$$$$$$$$$$$$$$$xXXX++++++++++$$$$$$$$$$$$$$$$$$$$$X$&$$XX$$$$$$$$$$$$X$$$$$$$$$$&&&$$$&&&&&&&&&&&&&&$$&&&&$$$$$$$$$$$$$$$$$XX$$&$$$$$$$$$$X",
"$&&&$$&$$$&&$$X$$$$$$$$$$$$$$$$$$$&&&&&&$$&&&&$$$X$XX$$$$XXX$$$$$$$$$$$$$$$$$$$$$XXXXXX++++++++++++$$$$$X$$$$$$$$$$$XX$$$$$$XXX$$$$$$$$$$XX$$$$$$$$$$$$$&$&&&&&&&&&&&&&&&&&&&&$$$$$$$&&$&&&$$$$$X$$$$XXX$$$$XXX",
"$$&&$$$$$$$&&$$$$$$&&$$$$$$$$$$$$$&&&&&&&&&&$$$$$$xX$$$XXXXX$$$$$$$$$$$$$$$$$$$XXXXXXx++++++++++++++++X$$$$$$$$$$$$$XXX$$$$XXXXX$X$$$$$$$XX++++++++xxX$$$$$&&&&&&&&&&&&&&&&&$$$$$$$$$$$$$$&$X$$$XXXX$$$$$$$$$$$",
"$$$$$$$$$$$$&$$$$$$$$$$$$X$$$$$$$&&&$$&&&&$$&$$$$xX$$$$$xxxXX$$$$$$$$$$$$$X$$$$XXXXX++;;+;+;+;++++;+;+;X+xX$$$$$$$$$XXXXX$$XXXX$$X$$$$$$X$Xx++++++++++xxX$$$$$$$&$$&&&&&&&$$$$$&$$$&&$$$&$$$$$$XXXXXX$XXX$$XXX$",
"$$&&$$$$$$$$&&&$$$$$$$$&&&&&$$$$$$&$$$$$$&$$$$$$+XXXX$$XxxXX$$$$$$$$$$$$XX$$XXXXXx++++++;+++++;+++++++x+++XX$$$$$$XXXXXXXXXX$XX$$X$$X$$$XXXXx+++++++++++xx$$$$$$$$$&&&$$$$$$&&$$$&&&&$&&&&$$XX$$$$$XXX$XX$$$$$$",
"&$$$&&&$&$$&&&&&$$&$$$&&$$$$$&&&$$$&&$$&&&&$&&XxXXXX$$$xxxX$$$$XX$XXXXXXxX$$Xx++;++++;;++;+;++++;+++++++++X$$$X$$$XXXX$XXXXX$XX$$X$$$XX$$XXXXXx++++++++++xxX$$$$$$$&&&X$XXX$&&&&&&&&&&&&&$$$$$$$X$$$$$$$$$&$&$$",
"&&&$$$$&&&&$$&&$$$$&$$&&&$$$$&&&&$$&&&$$$$$$$xXXXXXX$$X+x$$$$XXX$XXXXXX+X$X+++;;;;;;;+;;+;+;+++;+;+++++++xX$$$$$$$$XXX$$$$$$X$X$$$$X$XXXX$$XXXXXXx+++++xxxxxx$$$$$&&&$XxXXX$&&&&$&&$$$&&&$$$$$$$$$$$$$$$$$$$$$&",
"&&&&&$$$&&&$$&&&$$$$$$$&&&&&&&&&&&&&$$$$$&&$x+XXXXXxxxxxX$$XXxXXXxXXXXxXXX+++;+;++;+;+++;+;+;+;++;+++++++xX$$X$$$$$$X$XX$$$$$$$$$$XXXX$XXX$$$$X$XXXxxxxxxxxxXX$X$$&&&&&&&&&&&&&$$&&$$$$$$$$$$$$$&$&&$$$$$$$$$$$",
"&&&&&&$$$&&&$&&&$&$&&&&&&&&&&&&&&&$$$$XXX$x+XXxx++++++xX$$$XxXXXXXx+XXX+xx+;+;+;;;+;+;+;+;+;+;+;+;+++++++x$$XX$$$$$$X$$$X$$$$$$X$$XXXXXXXXXX$$$$XXXXXXXXxXXXXXXX$$$$$&&$$&$&&$$$$$$$$$$&$$$&&&&&&&&&$$$$$$$$$$$",
"&&&$&&&$$&&&$$&&&$$&&&&&&&$$$$&&&$$XXXXXXxXXx++++;;++xXX$$XXxXXxXx+XXx;;++;;;+;;;;+;+;+;++;+++;+;++++++++xXXXX$$$$$$$$$$$$$$$XXXX$XXXXXXXXXX$$$$$X$XXXXXXXXXXXXXX&&&&$&&$$$$&$$$$$XXX$$&$$$$&&&&&$&&$$$$$$$$$$$",
            };
            CursorPosition(0, 0);
            foreach (string x in strings)
            {
                CursorPosition(1, Console.CursorTop + 1);
                Console.Write(x);
                if (Console.CursorTop == PlannedHeight - 2)
                {
                    break;
                }
            }
        }

        // Prints out the empty frame 
        public static void CleanFrame()
        {
            Console.ForegroundColor = Foreground;
            Console.BackgroundColor = Background;
            CursorPosition(0, 0);
            Console.Write(Frame);
        }
        // Frame for input box - customizable text, size, position etc
        public static void InputFrame(string header = "", int size = 0, int posx = 0, int posy = 0, bool clear = false)
        {
            size = size == 0 ? PlannedWidth - posx : size;
            if (clear)
            {
                Console.ForegroundColor = Background;
                Console.BackgroundColor = Background;
            } else
            {
                Console.ForegroundColor = Foreground;
                Console.BackgroundColor = Background;
            }
            CursorPosition(posx, posy);
            if (header == "")
            {
                Console.Write("+" + new string('-', size - 2) + "+");
                CursorPosition(posx, posy + 1);
                Console.Write("|" + new string(' ', size - 2) + "|");
                CursorPosition(posx, posy + 2);
                Console.Write("+" + new string('-', size - 2) + "+");
            } else
            {
                header = " " + header + " ";
                if (header.Length > size - 20)
                {
                    header = header.Substring(0, size - 20);
                }
                Console.Write("+" + new string('-', header.Length) + "+" + new string('-', size - (header.Length + 3)) + "+");
                CursorPosition(posx, posy + 1);
                Console.Write("|" + header + "|" + new string(' ', size - (header.Length + 3)) + "|");
                CursorPosition(posx, posy + 2);
                Console.Write("+" + new string('-', header.Length) + "+" + new string('-', size - (header.Length + 3)) + "+");
            }
            return;
        }
        // Game frame - WIP
        public static void GameFrame()
        {
            Console.Clear();
            Console.ForegroundColor = Foreground;
            Console.BackgroundColor = Background;
            CursorPosition(0, 0);
            Console.WriteLine("+--------------------------------------------------------------+--------------------------------------------------------------+");
            Console.WriteLine("|                                                              |                                                              |");
            Console.WriteLine("|  Player 1:                                                   |  Player 2:                                                   |");
            Console.WriteLine("|                                                              |                                                              |");
            Console.WriteLine("|  Statistics:     Lives    Strength    Mana    Flexibility    |  Statistics:     Lives    Strength    Mana    Flexibility    |");
            Console.WriteLine("|                                                              |                                                              |");
            Console.WriteLine("+--------------------------------------------------------------+--------------------------------------------------------------+");
            for (int i = 0; i < 12; i++)
            {
                Console.WriteLine("|                                                                                                                             |");
            }
            Console.WriteLine("+-------------+---------------------------------------------------------------------------------------------------------------+");
            Console.WriteLine("|             |                                                                                                               |");
            Console.WriteLine("|  Message:   |                                                                                                               |");
            Console.WriteLine("|             |                                                                                                               |");
            Console.WriteLine("|             |                                                                                                               |");
            Console.WriteLine("|  Action:    |                                                                                                               |");
            Console.WriteLine("|             |                                                                                                               |");
            Console.WriteLine("|             |                                                                                                               |");
            Console.Write("+-------------+---------------------------------------------------------------------------------------------------------------+");
        }
        #endregion
        #endregion
        #region Input module
        // KeyInput, takes priority in consideration to only give input to the correct module
        public static ConsoleKeyInfo KeyInput(int priority, int? posx = null, int? posy = null, bool mustEnter = false)
        {
            if (posx is not null)
            {
                CursorPosition(posx.Value, Console.CursorTop);
            }
            if (posy is not null)
            {
                CursorPosition(Console.CursorLeft, posy.Value);
            }
            ChannelReader<ConsoleKeyInfo> reader = keyTunnel.Reader;
            ConsoleKeyInfo pressedKey;
            while (true)
            {
                while (reader.TryRead(out pressedKey))
                {
                    if (((mustEnter && pressedKey.Key == ConsoleKey.Enter) || (!mustEnter)))
                    {
                        return pressedKey;
                    }
                }
                while (priority > Priorized) { Thread.Sleep(10); };
            }
        }
        #endregion
        #region Data module
        // Centers a string or shortens a string when larger than width
        public static string StringFormat(string content, int width, string align = "center")
        {
            if (content.Length > width)
            {
                if (align == "left")
                {
                    return content.Substring(content.Length - width, width);
                }
                else
                {
                    return content.Substring(0, width);
                }
            }
            else if (content.Length == width)
            {
                return content;
            }
            else
            {
                switch (align)
                {
                    case "left":
                        return content + new string(' ', width - content.Length);
                    case "right":
                        return new string(' ', width - content.Length) + content;
                    default:
                        return new string(' ', (width - content.Length) / 2) + content + new string(' ', (width - content.Length) / 2 + (width - content.Length) % 2);
                }
            }
        }
        // Calculate position of an element
        /// <summary>
        /// Calculates and offset location of an element. made for easier positioning of elements. To offset from center, use negative values to go left/uo.
        /// </summary>
        /// <remarks>
        /// How have i not made this before adding a lite version of this to EVERY ELEMENT DEFINITION smh.
        /// </remarks>
        public static Tuple<int, int> CalculatePosition(int contentSize, int offx, int offy, string shiftx = "center", string shifty = "center")
        {
            int posx, posy;
            switch (shiftx)
            {
                case "left":
                    posx = offx;
                    break;
                case "right":
                    posx = PlannedWidth - contentSize - offx;
                    break;
                default:
                    posx = (PlannedWidth - contentSize) / 2 + offx;
                    break;
            }
            switch (shifty)
            {
                case "up":
                    posy = offy;
                    break;
                case "down":
                    posy = PlannedHeight - offy;
                    break;
                default:
                    posy = (PlannedHeight - offy) / 2 + offy;
                    break;
            }

            return new Tuple<int, int>(posx, posy);
        }   
        // Sending info to writer
        public class WriterInfo
        {
            public string content;
            public ConsoleColor fore, back;
            public int posx, posy;
            public WriterInfo(string content, int posx, int posy, ConsoleColor fore, ConsoleColor back)
            {
                this.content = content;
                this.posx = posx;
                this.posy = posy;
                this.fore = fore;
                this.back = back;
            }
        }
        // Prints out a string of color
        public static void ColorString(string content, ConsoleColor? fore = null, ConsoleColor? back = null)
        {
            if (fore == null)
            {
                fore = Foreground;
            }
            if (back == null)
            {
                back = Background;
            }
            Console.ForegroundColor = fore.Value;
            Console.BackgroundColor = back.Value;
            Console.Write(content);
            Console.ForegroundColor = Foreground;
            Console.BackgroundColor = Background;
        }
        
        #endregion
    }
}
