[英文教程：http://krpc.github.io/krpc/](http://krpc.github.io/krpc/)

# KRPC Controller

- 这整个项目其实是一个仿 Unity 的框架，有较强的可拓展性。
- 主要看 ConnectionInitializer 类和 Behaviours 文件夹内的几个类。 ConnectionInitializer 类里面是初始化的组件， Behaviours 文件夹里则是放组件类的地方。

## Quick Start 0
于2019.03.02(YYYY.MM.DD)添加了转发安卓设备模拟摇杆数据的功能，请配合[KSP_AndroidJoystick](https://github.com/xdedss/KSP_AndroidJoystick)食用

## Quick Start 1
- 建议游戏调成窗口模式
- 游戏中安装 KRPC 的 Mod 
https://spacedock.info/mod/69/kRPC
- 游戏中打开 Mod 的服务器
- 让一个类似火箭的东西从空中下落，SAS打开对准地表速度反方向
- 在程序初始化部分给载具附加SoftLanding组件，运行程序，点击Connect连接到游戏
- 应该可以看到窗口出现，到一定高度时开始反推，然后落地

#  使用方法

[官方教程](http://krpc.github.io/krpc/)中给出的方法大都是从程序主入口顺着往下写，如果只需处理一个过程（如监控燃料烧完时分级）那么还算比较简单，但是如果需要同时处理两个过程（比监控燃料的同时检测高度调整姿态）就会很麻烦，写出一些奇怪的结构

## 界面

Winform中Connect按钮是与游戏中的kRPC服务器连接（默认参数）
Pause是在连接后暂停实时输出的数据
Socket是打开Socket服务器监听，准备和手机上的摇杆程序连接。

## Behaviour 系统

- 为了能愉快的写代码，我仿了一套Unity的**伪·多线程**系统，虽然并没有Unity的源码，但是自己瞎写一通基本实现了相似的功能
- 在这里基类叫做 Behaviour （类似Unity的MonoBehavioiur）
- 所有继承 Behaviour 的组件都**必须添加到载具上**才能工作
- 可以在**ConnectionInitializer**类里面初始化的时候用 **vessel.addComponent\<T\>()** 把继承了 Behaviour 的类实例添加到载具上，类似于 MonoBehaviour 添加到 GameObject 上
- 继承了 Behaviour 的类实现**Start**和**Update**方法，Update每帧调用
- 继承了 Behaviour 的类实例可以访问vessel字段获取载具信息和操作载具
- 继承了 Behaviour 的类实例可以**StartCoroutine**，参数为IEnumerator（和Unity操作基本一致）
- 继承了 Behaviour 的类实例可以**AddComponent**、**GetComponent**与同一载具或不同载具上的其它组件互动
- **Log()** 在窗口下半部分文本框中输出单条信息
- **LogInfo()** 每一帧都调用一次，就可以在窗口上半部分文本框中持续输出动态的信息
- 可以调用 **Time.deltaTime** 、 **Time.gameDeltaTime** 获取前一帧的时间
- 可以调用 **Input.GetKeyDown** 等获取键盘输入（是form上的键盘输入而非游戏中的）

## 如何写一个新的Behaviour

- 继承 Behaviour 类并实现 Start 和 Update 方法
- Start 在刚开始时调用一次，Update 每帧调用
- 可以选择实现一些其它 virtual 的方法
- 写上自己的逻辑
- 不要忘了在 ConnectionInitializer 类里初始化的时候给 vessel 添加这个组件

## 一些类的解释

### class CommonDataStream

- 继承自Behaviour，也是一个可以附着到vessel上的组件
- 当需要每一帧频繁调用某个数据时就需要用这个，以优化通讯效率
- 实例上有GetPisition、GetRotation等方法，调用时会自动建立一个流，长时间不调用会自动关闭流

### class SoftLanding

- 非常简单的着陆 Demo，用匀加速直线运动公式计算落地所需推力
方法，调用时会自动建立一个流，长时间不调用会自动关闭流

### class Ext

- 给Vessel类拓展了一些东西如AddComponent
- 还有CelestialBody、Orbit、Tuple<一堆T> 类的拓展

