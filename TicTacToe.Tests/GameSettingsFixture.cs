using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe.Tests;

internal class GameSettingsFixture : IOptionsMonitor<GameSettings>
{
    public GameSettings CurrentValue { get; }

    public GameSettingsFixture(GameSettings gameSettings)
    {
        CurrentValue = gameSettings;
    }

    public GameSettings Get(string? name)
    {
        throw new NotImplementedException();
    }

    public IDisposable? OnChange(Action<GameSettings, string?> listener)
    {
        throw new NotImplementedException();
    }
}
