# MouseLimiter 鼠标限制器

\n\n\n

普通用户直接下载 exe 即可使用

\n\n\n\


本软件开发动机为：想要这个功能，但网上现有的软件太贵了要100。想着功能简单我就自己开发了个 。

由于该版本使用WPF进行开发，故而软件本体偏大。

基于WPF开发，原理为：

创建隐形窗口，并联动windows窗口钩子函数限制鼠标必须在窗口内以做到限制鼠标

其实后面也做了C语言版本的，但c自带的GUI太讨厌了一直乱码，换成英文后却无法使用了..遂搁置



项目核心文件

---`MainWindow.xaml`  软件ui

---`MainWindow.xaml.cs`  软件逻辑

---`MouseRangeLimiter.csproj ` 编译解析文件



关于快捷键修改：

在文件` MainWindow.xaml.cs` 下的方法`CheckKeyboardShortcuts`修改 ，分别为 坐标保存 与 限制启用

具体虚拟键码自行搜索
