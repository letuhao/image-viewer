# Script to clear RabbitMQ queues
Add-Type -Path "C:\Program Files\RabbitMQ Server\rabbitmq_server-3.12.10\lib\rabbitmq_client-3.12.10\lib\RabbitMQ.Client.dll"

try {
    # Create connection factory
    $factory = New-Object RabbitMQ.Client.ConnectionFactory
    $factory.HostName = "localhost"
    $factory.Port = 5672
    $factory.UserName = "guest"
    $factory.Password = "guest"
    
    # Create connection and channel
    $connection = $factory.CreateConnection()
    $channel = $connection.CreateModel()
    
    # List of queues to purge
    $queues = @(
        "bulk-operation-consumer",
        "collection-scan-consumer", 
        "image-processing-consumer",
        "cache-generation-consumer",
        "thumbnail-generation-consumer"
    )
    
    Write-Host "Purging RabbitMQ queues..."
    
    foreach ($queue in $queues) {
        try {
            $purgedCount = $channel.QueuePurge($queue)
            Write-Host "Purged $purgedCount messages from queue: $queue"
        }
        catch {
            Write-Host "Could not purge queue $queue : $($_.Exception.Message)"
        }
    }
    
    # Close connection
    $channel.Close()
    $connection.Close()
    
    Write-Host "Queue purging completed!"
}
catch {
    Write-Host "Error connecting to RabbitMQ: $($_.Exception.Message)"
    Write-Host "Make sure RabbitMQ is running and accessible on localhost:5672"
}
