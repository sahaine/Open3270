namespace SampleForm
{
   using Open3270;
   using System;
   using System.Diagnostics;
   using System.Drawing;
   using System.Windows.Forms;

   public class OpenEmulator : RichTextBox
   {
      private readonly TNEmulator TN3270 = new TNEmulator();

      private bool IsRedrawing;

      private Size _inital;


      public void Connect(string Server, int Port, string Type, bool UseSsl)
      {
         

         TN3270.Config.UseSSL = UseSsl;
         TN3270.Config.TermType = Type;
         TN3270.Connect(Server, Port, string.Empty);

         Redraw();
      }

      public void Redraw()
      {
         var Render = new RichTextBox(); // { Text = TN3270.CurrentScreenXML.Dump() };

         for (var i = 0; i < TN3270.CurrentScreenXML.CY; i++)
         {
            Render.Text += TN3270.CurrentScreenXML.GetRow(i).PadRight(TN3270.CurrentScreenXML.CX, ' ') + "\r\n";
         }

         Clear();

         var fnt = new Font("Consolas", 10);
         Render.Font = new Font(fnt, FontStyle.Regular);

         IsRedrawing = true;
         Render.SelectAll();

         if (TN3270.CurrentScreenXML.Fields == null)
         {
            var clr = Color.Lime;
            Render.SelectionProtected = false;
            Render.SelectionColor = clr;
            Render.DeselectAll();

            for (var i = 0; i < Render.Text.Length; i++)
            {
               Render.Select(i, 1);
               if (Render.SelectedText != " " && Render.SelectedText != "\n")
               {
                  Render.SelectionColor = Color.Lime;
               }
            }

            return;
         }

         foreach (var field in TN3270.CurrentScreenXML.Fields)
         {
            // if (string.IsNullOrEmpty(field.Text))
            // continue;
            Application.DoEvents();
            var textClr = Color.Lime;
            var backClr = Color.Black;
            var protect = true;

            if (field.Attributes.FieldType == "High" && field.Attributes.Protected)
            {
               textClr = Color.White;
            }
            else if (field.Attributes.FieldType == "High")
            {
               textClr = Color.Red;
               backClr = Color.LightGray ;
               protect = false;
            }
            else if (field.Attributes.FieldType == "Hidden")
            {
               textClr = Color.Black ;
               backClr = Color.LightGray;
               protect = false;
            }
            else if (field.Attributes.Protected)
            {
               textClr = Color.RoyalBlue;
            }

            Debug.WriteLine(field.Attributes.FieldType);

            Render.Select(field.Location.position + field.Location.top, field.Location.length);
            Render.SelectionProtected = false;
            Render.SelectionColor = textClr;
            Render.SelectionBackColor = backClr;

            if (protect)
            {
               Render.SelectionProtected = true;
            }
         }

         for (var i = 0; i < Render.Text.Length; i++)
         {
            Render.Select(i, 1);
            if (Render.SelectedText != " " && Render.SelectedText != "\n" && Render.SelectionColor == Color.Black)
            {
               Render.SelectionProtected = false;
               Render.SelectionColor = Color.Lime;
            }
         }

         this.Rtf = Render.Rtf;

         IsRedrawing = false;
      }

      protected override void OnKeyDown(KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Back)
         {
            this.SelectionStart--;
            e.Handled = true;
            return;
         }

         if (e.KeyCode == Keys.Tab)
         {
            // TN3270.SetCursor();
            e.Handled = true;
            return;
         }
      }

      protected override void OnKeyPress(KeyPressEventArgs e)
      {
         if (e.KeyChar == '\r')
         {
            TN3270.SendKey(true, TnKey.Enter, 1000);
            Redraw();
            e.Handled = true;
            return;
         }

         if (e.KeyChar == '\b')
         {
            return;
         }

         if (e.KeyChar == '\t')
         {
            return;
         }

         TN3270.SetText(e.KeyChar.ToString());
         base.OnKeyPress(e);
      }

      protected override void OnSelectionChanged(EventArgs e)
      {
         if (TN3270.IsConnected)
         {
            base.OnSelectionChanged(e);
            if (!IsRedrawing)
            {
               int i = this.SelectionStart, x, y = 0;
               while (i >= 81)
               {
                  y++;
                  i -= 81;
               }

               x = i;

               TN3270.SetCursor(x, y);
            }
         }
      }

      protected override void OnSizeChanged(EventArgs e)
      {

         if (_inital == Size.Empty)
         {
            _inital = Size;
            return;
         }

         var vZoom = Height / (float)_inital.Height;
         var hZoom = Width / (float)_inital.Width;

         ZoomFactor = (hZoom > vZoom ?  vZoom : hZoom);
      }
   }
}
