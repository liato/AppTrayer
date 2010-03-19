AppTrayer
=========

AppTrayer lets you minimize any application that you start with AppTrayer
to the system tray.

The application is not capable of putting already running applications
in the system tray.



Usage
-----

Minimizing a long running python script to the system tray with a custom icon:

    apptrayer.exe --icon=C:\\path\\to\\icon.ico "C:\\python26\\python.exe" "C:\\path to\\my\\script.py"
   

Options
~~~~~~~

    --icon=<path>   Show a custom icon instead of the application icon.
    --minimize      Start the application minimzed.
    
    
License
-------

MIT.
