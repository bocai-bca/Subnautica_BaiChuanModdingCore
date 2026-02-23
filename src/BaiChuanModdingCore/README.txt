### 本地配置说明
请在下载、克隆本库之后，在本自述文本的相同位置处，找到`LocalReferences.props.template`，复制一份并重命名为`LocalReferences.props`。  
然后打开`LocalReferences.props`文件，将其中的`BepInExCore`和`SubnauticaData`标签分别设置为指向你本地环境中的`.../BepInEx/core`目录和`Subnautica_Data`的路径。  
`LocalReferences.props`包含在`.gitignore`中，请不要向仓库推送该文件。