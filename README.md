# SharpAliyunDDNS

### 一个基于c#的阿里云云解析DDNS

> 这是一个可以调用阿里云DNS解析服务的程序
> 
> >每分钟刷新获取自身的公网IP地址
>
> >检测到IP地址不同时就会更新云端的解析记录
>
> 他在启动时会检测云端是否有记录
>
> 如果没有Dns记录就会先添加记录
>
> >如果有记录检测到IP地址不符就会更新记录
>
> 配置文件才用Json数据，具体格式如下
>
> {
>  "data": \[
>
>    "域名,主机记录,Ttl",
>
>    "域名,主机记录,Ttl"
>
>  ],
>
>  "Key": {
>
>    "AccessKeyId": "请输入AccessKeyId",
>
>    "AccessKeySecret": "请输入AccessKeySecret"
>
>    }
>
>}
