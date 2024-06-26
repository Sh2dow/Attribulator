﻿using System;
using System.Collections.Generic;
using System.Linq;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript
{
    public class ModScriptService : IModScriptService
    {
        private readonly Dictionary<string, Func<string, IModScriptCommand>> _commandMappings =
            new Dictionary<string, Func<string, IModScriptCommand>>();

        public IEnumerable<IModScriptCommand> ParseCommands(IEnumerable<string> commands)
        {
            var lineNumber = 0L;
            foreach (var command in commands.Select(s => s.Trim()))
            {
                lineNumber++;
                var newCommand = this.ParseCommand(command, lineNumber);
                if (newCommand != null)
                {
                    yield return newCommand;
                }
            }
        }

        public IModScriptCommand ParseCommand(string command, long lineNumber)
        {
            if (string.IsNullOrEmpty(command)) return null;
            if (command.StartsWith("#", StringComparison.Ordinal)) return null;

            var parts = command.Split('"')
                .Select((element, index) => index % 2 == 0 // If even index
                    ? element.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries) // Split the item
                    : new[] {element}) // Keep the entire item
                .SelectMany(element => element).ToList();

            for (var index = 0; index < parts.Count; index++)
            {
                var part = parts[index];
                if (part.StartsWith("0x", StringComparison.Ordinal))
                    parts[index] = $"0x{part.Substring(2).ToUpper()}";
            }

            // Find command
            if (_commandMappings.TryGetValue(parts[0], out var creator))
            {
                var newCommand = creator(command);
                newCommand.LineNumber = lineNumber;

                try
                {
                    newCommand.Parse(parts);
                }
                catch (Exception exception)
                {
                    throw new CommandParseException($"Failed to parse command at line {lineNumber}: {command}",
                        exception);
                }

                return newCommand;
            }
            else
            {
                throw new CommandParseException($"Unknown command: {parts[0]} (line {lineNumber} [{command}])");
            }
        }

        public void RegisterCommand<TCommand>(string name) where TCommand : IModScriptCommand, new()
        {
            _commandMappings.Add(name, line => new TCommand {Line = line});
        }

        public IEnumerable<string> GetAvailableCommandNames()
        {
            return _commandMappings.Keys;
        }
    }
}