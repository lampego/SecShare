using Spectre.Console;

AnsiConsole.Progress()
    .Columns(
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new DownloadedColumn(),
        new TransferSpeedColumn(),
        new RemainingTimeColumn())
    .Start(ctx =>
    {
        var task = ctx.AddTask("game-installer.exe", maxValue: 524288000); // 500 MB in bytes
  
        while (!ctx.IsFinished)
        {
            task.Increment(2621440); // 2.5 MB per tick
            Thread.Sleep(50);
        }
    });
