Attribulator.CLI pack -i "C:\Users\Despe\Desktop\Attribulator\Unpacked\Main\attributes\db" -o C:\Users\Despe\Desktop -p CARBON


.\Attribulator.CLI.exe apply-script -i Unpacked -o Packed -p CARBON -s C:\path\to\scripts\folder


Attribulator.CLI pack -i "d:\Games\NFSC Mod\GLOBAL" -o C:\Users\Despe\Desktop -p CARBON

.\Attribulator.CLI.exe apply-script -i  -i "d:\Games\NFSC Mod\GLOBAL" -o "UnPack" -p CARBON -f yml



.\Attribulator.CLI.exe unpack -i "d:\Games\NFSC Mod\GLOBAL" -o "d:\Games\NFSC Mod\GLOBAL\Out" -p CARBON -f yml

unpack
  -i           Required. Directory to read BIN files from

  -o           Required. Directory to write unpacked files to

  -p           Required. The profile to use

  -f           Required. The format to use

  --help       Display this help screen.

  --version    Display version information.



.\Attribulator.CLI.exe apply-script -i Unpacked -o Packed -p CARBON -s C:\path\to\scripts\folder


  plugins              List the loaded plugins.

  profiles             List the loaded profiles.

  formats              List the available storage formats.

  pack                 Pack a database to BIN files.

  unpack               Unpack binary VLT files.

  generate-hashlist    Generate a hash-source list file from an unpacked database.

  apply-script         Apply a ModScript to a database.

  apply-script-bin     Apply a ModScript to a compiled database.

  script-commands      List the available ModScript commands.

  help                 Display more information on a specific command.

  version              Display version information.
  
  
unpack_stream "TRACKS\L5RA.BUN" "TRACKS\STREAML5RA.BUN" "f:\NFS\NFSC_Mods\W2C\STREAML5RA_PP\Unpacked"
pack_stream "TRACKS\L5RA.BUN" "TRACKS\STREAML5RA.BUN" "f:\NFS\NFSC_Mods\W2C\STREAML5RA_PP\Unpacked"

+380 95 252 9086