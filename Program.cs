using System;
using syncsptlrc.Services;
using syncsptlrc.Views;
using syncsptlrc.Presenters;

class Program
{
    static void Main(string[] args)
    {
        var player = new PlayerService();
        var ascii = new AsciiService();
        var lyrics = new LyricsService();
        var view = new ConsoleView();

        var presenter = new MainPresenter(view, player, ascii, lyrics);
        presenter.Run();
    }
}