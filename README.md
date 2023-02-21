# English version

# About

This is a set of two tools allowing you to setup world-locked items
onto your avatar.
These items can then be placed and put out, using buttons added to
the Expression menu.
These items won't move with the avatar while placed, hence the name
"world-locked" items.

For more explanation, check the following videos

# Disclaimer

These tools are designed to not touch the original avatar.
They make a copy of any asset requiring modifications
(avatar, animators, ...), and only perform modifications on these
copies.

Still, as always, backup the items and avatars you'll use with
these tools, since nasty bugs can still arise.  
I only tested it on MY avatars.  
So far, so long, no problems arised, but you might encounter bugs
I haven't.

If using these tools change anything on your original avatar, feel
free to open a bug report.

# Videos　explanations

## Setup world-locked items on PC avatars, using the Constraints tool

[wlas-1.4-setup-constraints.webm](https://user-images.githubusercontent.com/84687350/219586892-9cdb912f-4e09-4948-bf6c-1295f8ff1a3d.webm)

[wlas-1.4-constraints.webm](https://user-images.githubusercontent.com/84687350/219586914-7fc0956e-da5e-4657-bbb7-02509fa84127.webm)

## Setup world-locked items on Quest avatars, with the Particles tool

[wlas-1.4-unity-setup-particles.webm](https://user-images.githubusercontent.com/84687350/219586982-2de14540-5376-4a00-9313-91b1991bcc2a.webm)

[wlas-1.4-particles.webm](https://user-images.githubusercontent.com/84687350/219587013-d0eb1cda-0710-4b98-91e7-f1cce6fca5b7.webm)

## Video credits

* Avatar : 2A-7-4 / RRRR　ヨツル by 拾い部屋  
  https://hiroiheya.booth.pm/items/2019040
  
* Item : Low poly (midi) keyboard  
  https://sketchfab.com/3d-models/low-poly-midi-keyboard-50040134cbfe4268a9af8b074bd24b15
  
# Requirements

* Unity
* VRChat Avatar SDK 3.0

# Download

See https://github.com/vr-voyage/vrchat-worldlock-autosetup/releases

# Install

## If you're upgrading from version 1.3 or earlier, remove the old version

[wlas-1.4-uninstall-old-package.webm](https://user-images.githubusercontent.com/84687350/220350032-8375383f-80ed-4ba6-b676-384628e4c126.webm)

Just locate the folder 'Voyage' in your Assets and delete it.  
Then close Unity.

Note that you can remove it afterwards too.

## Using the **.unitypackage** file

[wlas-1.4-install-unity-package.webm](https://user-images.githubusercontent.com/84687350/219587661-29fd674c-3c80-47b6-b8f8-5a57e5f7629f.webm)

* Install **world-lock-autosetup-1.4.0.unitypackage** using the editor menu **Assets > Import Package... > Custom Package...**.

## Using the VRChat Creator Companion

[wlas-1.4-install-vpm-user-package.webm](https://user-images.githubusercontent.com/84687350/219587822-cc321ec8-ab67-46a3-b9c1-53672a5cf15d.webm)

### Setup VCC to find the VPM package

* Download the sources of this repository and unpack them somewhere on your harddrive
* Open the **VRChat Creation Companion**
* In **Settings**, Click the `Add` Button in **User Packages**
* Select the folder where the downloaded sources were unpacked

### Use the package in your project

* In a **Project**, press the `Add` button below **Voyage's WOrld Lock Autosetup** in the list.

# Usage
  
## How to use the Constraints tool

Works with PC avatars only.
**Constraints** components are not allowed on Quest avatars.

* Add your avatar to the Scene root.
* For good measures, make sure that your avatar position and rotation
  are set to 0,0,0.
* Add the items to the Scene root.
* Set the rotation and position of the items, in relation to your
  avatar, to define how it should appear when shown.
* The **Expression menu** buttons will show the item names as set
  in the **Hierarchy**.  
  So, set those names accordingly.

* Open the setup window
  **Voyage > World Lock Setup - Constraints (PC)**.
* Click on the 'RESET' button, at the upper left of that window.
* Drag your avatar and the item to setup from the **Hierarchy**,
  and drop them inside that window.
* Click on the 'APPLY' button, that should have appeared at the
  bottom of that window.

You can then upload the clone, and use it in-game to spawn the
items you just setup.

This new clone will have a new submenu "World Objects" added to
its **Expressions menu**.
This menu contains Toggle buttons allowing you to spawn each
individual object.  

Note:
- You can setup up to 8 individual items (Only 8 items for now,
  due to menu creation limitations).
- When placed, the object follows the avatar orientation.

## How to use the Particles tool

Works with PC and Quest avatars.

* Add your avatar to the Scene root.
* For good measures, make sure that your avatar position and rotation are set to 0,0,0.
* Add the item to the Scene root.
* Move the added item where you'd like to see it spawned, in relation to your avatar.

* Open the setup window **Voyage > World Lock Setup - Particles (PC or Quest)**.
* Click on the 'RESET' button, at the upper left of that window.
* Drag your avatar and the item to setup from the **Hierarchy**, and drop them inside that window.
* Click on the 'APPLY' button, that should have appeared at the bottom of that window.

Once done, a new setup clone of the avatar will appear.

You can then upload the clone, and use it in-game to spawn the objects using the **Expression menu**.

This new clone main menu will have two new buttons in its **Expression menu** :
* Spawn - Which allows you to spawn the actual item.
* Rotate - Which allows you to define the orientation of the spawned element.

All the animations, animators, materials and meshes will be copied into a specific folder, named :
"[Avatar name]-ParticlesLock-[YearMonthDay]-[HourMinSeconds]-[Milliseconds]".
You can define where the folder is generated by dropping a folder on "Save Dir".

> You can actually drop a saved asset on "Save Dir".
> If you do so, "Save Dir" will be set to that asset folder.

### Setup the items to always show at a specific world position

Follow **Setup an avatar with items locked using Constraints**,
but before clicking on 'APPLY' in the setup window, check
the box **Lock from world (0,0,0)**.  
By doing so, the avatar will be setup so that the items always
spawn at specific world coordinates.

These coordinates being the "Position" of these items, in the
Editor, before clicking on 'APPLY'.

That means that you can setup a **World Overlay** using this
mechanism.

# More details about these tools

## Why make these tools

While plenty of documentation exist on how to setup world-locked items,
the process are all tedious, so I made two tools that automatically :

* Setup the hierarchy.
* Create the animations.
* Setup the FX animator (creating it if it's doesn't exist).
* Setup the SDK3 menu parameters.
* Add the SDK3 menu buttons.

## Why two tools

Each tool targets a different platform (PC or Quest) and use
a methodology best suited for that platform.

## Pros and cons of each tool

### Constraints

#### Pros

* The setup itself has no effect on the avatar rating.
* You can setup multiple items at once.
* Setup items can use any whitelisted component.

#### Cons

* Can only be used to setup PC avatars

### Particles

#### Pros

* Can be used to setup Quest avatars

#### Cons

* Due the Particle Emitter setup on the avatar,
  Quest avatars will see their rating drop to **Poor** or lower.  
  If the spawned item has more than 200 (two hundred) polygons,
  the rating will automatically downgrade to **Very Poor**.  
  You should take this seriously when it comes to Quest Avatars,
  since Quest clients are setup to always display "Fallback" avatars
  instead of **Very Poor** rated avatars.  
  Meaning that, if your avatar rating drops to **Very Poor**, other
  users won't see your setup avatar unless they manually choose to
  "Show your avatar".
* You can only use ONE material to define the look of the
  spawned item.  
  Note that, strangely enough, you CANNOT use Particles shaders
  with Particles systems set on Quest avatars.  
  Which means : No transparency.

# Licence

MIT

Meaning that, yes, You are free to incorporate this tool,
or parts of it, into other commercial goods, open source projects, ...

# FAQ

**Can I change the name of the files generated ?**
Yes.

**Can I edit the elements added to the Expression Menu without issues ?**
Changing the name and icons of the buttons will pose no issue.
However, avoid changing the parameters names, since the buttons
may stop working afterwards.

**I want to add another item to the avatar.**  
**Can I reuse the new generated avatar with these tools ?**
No.
If you want to add an item, just use the original avatar, along with
all the items you want to setup.  
The reason is that these tools can't tell the difference between
a non-setup avatar and a previously setup copy.  
Reusing a setup avatar (the generated copy) with these tools will
lead to some animation parameters being overwritten, causing multiple
issues with the animators.  



# 日本語版


# ワールド固定アイテム自動設定ツール

数クリックだけで、ワールドに固定できるアイテムをアバターへ自動設定する２つのツールです。  
装備したアバターのExpression Menuにボタンが追加され、そのボタンでアイテムを配置・収納できます。

詳しくは動画をご覧ください。

# 使い方

## 動画

### PCアバター向けのConstraintsツールの使い方

[![Constraintsの自動設定ツールの使い方](https://img.youtube.com/vi/hpTEYyTml0o/0.jpg)](https://www.youtube.com/watch?v=hpTEYyTml0o)

### Quest向けのParticlesツールの使い方

[![Particlesの自動設定ツールの使い方](https://img.youtube.com/vi/4kGonrsPCVg/0.jpg)](https://www.youtube.com/watch?v=4kGonrsPCVg)

# 忠告

そのツールは源アバターをコピーし、そのコピーを設定します。

にしても、万一、未知のバグのせいで、アバターやアイテムが改変される可能性がありますので、
そのツールを使用する前に、使うアセットをバックアップしてください。

アバターまたアイテムが改変された場合、不具合として報告してお願いします。

# 依存

* Unity
* VRChat Avatar SDK 3.0

# インストール方法

## 1.3以下からアップグレードをしている場合、まずは古いバージョンをアンインストールしてください

[wlas-1.4-uninstall-old-package.webm](https://user-images.githubusercontent.com/84687350/220350897-efe7eedf-7176-4bb8-8f1c-d7e66a2ce0fb.webm)

1.3以下のバージョンからアップグレードをしている場合、Assetsにある「Voyage」というフォルダーを削除してください。

1.4以上のパッケージはUnityの[パッケージ・システム](https://docs.unity3d.com/ja/current/Manual/PackagesList.html)を使用しています。  
そのため、ファイルのインストール先が1.3以下と違って、そのバージョンと衝突が起こります。

その問題を防ぐために、1.3以下のファイルを削除してください。

## **.unitypackage**を使いたい場合

[wlas-1.4-install-unity-package.webm](https://user-images.githubusercontent.com/84687350/219589062-405d5291-da24-49d7-838c-1e2503b88abe.webm)

* [**world-lock-autosetup-1.4.0.unitypackage**]のファイルをダウンロードします。
* Unityの「**Assets > Import Package... > Custom Package...**」の画面でパッケージ・ファイルを選んでインストールします。

## 「VRChat Creator Companion」パッケージを使いたい場合

[wlas-1.4-install-vpm-user-package.webm](https://user-images.githubusercontent.com/84687350/219589225-50f38167-6022-41d1-89d2-c2276876508a.webm)

* Sourcesのアーカイブをダウンロードします。
* ハードドライブのどこかで、ダウンロードしたアーカイブを解凍します。
* 「VRChat Creator Companion」の「Settings」にある、「User Packages」の「Add」ボタンを押下します。
* アーカイブの解凍先を選びます。

そうしたら、「Avatar」のプロジェクト画面の右パネルから、「Voyage's World Lock Autosetup」を「Add」（追加）することが出来ます。

# 詳しく

アイテムをワールドに固定するについては、多くの解説記事や動画が見つかります。  
しかし、いずれも手間暇がかりますから、この自動設定ツールを作りました。

このツールは自動的に：

* ヒエラルキーを用意し、
* アニメーションを生成し、
* FX Animatorを設定し、
* Expression parametersを設定し、
* Expression menuにボタンを追加します.

## なぜ2つのツールがありますか

それぞれのツールは、目指すプラットフォームに適した方法を採用しています。

## メリット・デメリット

### Constraints

#### 利点

* Ratingを低下しません。
* 複数アイテムを同時に装備させることが出来ます。
* アイテムに、いずれのVRCHATが許可するコンポネントを付けることが出来ます。

#### 欠点

* Quest対応アバターでは、Constraint系のコンポーネントは使用できませんので、  
  PCアバター限定。

### Particle System

#### 利点

* Quest対応アバターで使えます。

#### 欠点

* 追加するParticles Systemのため、Quest対応アバターのRatingがPoor以下に低下します。
* Quest対応アバターでは、200個のポリゴン以上のアイテムを設定する場合、Ratingが必ずVery poorに下がります。
その場合では、相手はShow Avatarしないと、そのアバターの代わりにフォールバックアバターを見ることになります。
* Particle System Rendererは一個のマテリアルしか使用できないため、アイテムの外観を一個のマテリアルで決定する必要があります。
* Quest対応アバターでは、Particles Systemでも、Particlesシェーダーを使用することができません。

## Constraintツールの使い方

Quest対応アバターでは、Constraint系のコンポーネントは使用できませんので、  
PCアバター限定。

* まずは、Sceneの中にSDK3.0のアバターを用意します。
* アイテムの配置場所がズレないように、
  アバターのPositionとRotationを0にします。
* 後は、Sceneに固定したいアイテムを追加します。
* アバターを基準として、アイテムのPositionとRotationを決めます。
* 後は、メニューバーで「Voyage > World Lock Setup - Constraints (PC)」を押下して、
  自動設定ツールの画面を開きます。
* 画面の左上にある「リセット」ボタンを押します。
* Hierarchyからアバターとアイテムをドラッグして、その画面の上にドロップします。
* 画面の下に現れた「適用」ボタンを押します。

すると、自動設定ツールは、アバターをコピーし、そのコピーにアイテムを設定します。
そのコピーをアップロードした後は、ゲームで着せる時、アイテムを配置・収納できます。

そのために、Expressionsメニューで、「World Objects」と言うサブメニューが追加されます。  
そこで、各アイテムを出すためのボタンがあります。  
ボタンを押下すると、関連のアイテムを配置します。  
もう一度押下すると、関連のアイテムを収納します。

## Particle Systemツールの使い方

PCとQuestアバター対応。
とはいえ、PCアバターの設定には、Constraintツールの利用を強くお勧めします。

* まずは、Sceneの中にSDK3.0のアバターを用意します。
* アイテムの配置場所がズレないように、
  アバターのPositionとRotationをゼロにします。
* 後は、Sceneに固定したいアイテムを追加します。
* アバターを基準として、アイテムのPositionを決めます。
* メニューバーで「Voyage > World Lock Setup - Particles (PC or Quest)」を押下して、
  自動設定画面を開きます。
* 画面の左上にある「リセット」ボタンを押します。
* Hierarchyからアバターとアイテムをドラッグして、その画面の上にドロップします。
* 画面の下に現れた「適用」ボタンを押します。

すると、自動設定ツールは、アバターをコピーし、そのコピーにアイテムを設定します。  
そのコピーをアップロードした後は、ゲームで着せる時、アイテムを召喚できます。

そのためにExpressionsメニューで、二つのボタンが追加されます：
* Spawn - アイテムを召喚・削除するボタン。
* Rotate - アイテムの方向を変えるボタン。

> Rotateボタンを使うと、アイテムは再度召喚され、位置が変わってしまいます。

> アバターの方向が変わっても、召喚されているアイテムの方向が変わりません。  
> アイテムの方法を変えたいなら、「Rotate」ボタンを使ってください。

# 質問

## 生成されたファイルの名前を変更することはできますか？
はい、問題なくファイルの名前を変更することができます。

## 生成されたコピーのメニューを編集することはできますか？
メニュー・アイテムの名前を変更することは問題ありません。
パラメータ名を変更すると、そのアイテムが動作しなくなることがありますので、変更しないでください。

## 新しいアイテムを追加するために、設定されたコピーをもう一度ツールで使うことはできますか
できません。
新しいアイテムを追加したい場合は、原アバターと装備したいアイテムをすべて揃えて、一からやり直してください。
ツールは原アバターと既に設定されているコピーを区別できません。
そのため、既に設定されているアバターを、ツールで再設定すると、Animatorのパラメータ名が上書きされてしまい、問題が発生します。
