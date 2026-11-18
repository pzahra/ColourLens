namespace ColourLens;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        if (!Native.MagInitialize())
        {
            MessageBox.Show("The Windows Magnification API could not be initialised.", "ColourLens",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            Application.Run(new FrameWindow());
        }
        finally
        {
            Native.MagUninitialize();
        }
    }
}
